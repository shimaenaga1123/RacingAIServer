using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Racing.Api.Realtime;
using Racing.Application.Competitions;
using Racing.Application.Leaderboard;
using Racing.Application.Orchestration;
using Racing.Application.Persistence;
using Racing.Domain.Competitions;

namespace Racing.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class CompetitionsController(
    CompetitionManager competitionManager,
    IRaceOrchestrator raceOrchestrator,
    SpectatorBroadcaster spectatorBroadcaster,
    IRaceResultRepository resultRepository) : ControllerBase
{
    private readonly CompetitionManager _competitionManager = competitionManager;
    private readonly IRaceOrchestrator _raceOrchestrator = raceOrchestrator;
    private readonly SpectatorBroadcaster _spectatorBroadcaster = spectatorBroadcaster;
    private readonly IRaceResultRepository _resultRepository = resultRepository;

    public sealed record CreateCompetitionRequest(
        string Name,
        string TrackId = "beginner-circuit",
        int? TotalLaps = null,
        int? MaxParticipants = null);

    public sealed record JoinCompetitionRequest(
        string AgentName,
        string SdkVersion);

    [HttpPost]
    public IActionResult Create([FromBody] CreateCompetitionRequest request)
    {
        var rules = new CompetitionRules
        {
            TotalLaps = request.TotalLaps ?? 3,
            MaxParticipants = request.MaxParticipants ?? 8
        };

        var comp = _competitionManager.CreateCompetition(
            name: request.Name,
            trackId: request.TrackId,
            rules: rules
        );

        return CreatedAtAction(nameof(GetByCode), new { code = comp.Code }, new
        {
            id = comp.Id,
            code = comp.Code,
            name = comp.Name,
            trackId = comp.TrackId,
            status = comp.Status.ToString(),
            rules = comp.Rules
        });
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var competitions = _competitionManager.GetAll().Select(comp => new
        {
            id = comp.Id,
            code = comp.Code,
            name = comp.Name,
            trackId = comp.TrackId,
            status = comp.Status.ToString(),
            rules = comp.Rules,
            participantCount = comp.Participants.Count
        });

        return Ok(competitions);
    }

    [HttpGet("{code}")]
    public IActionResult GetByCode(string code)
    {
        if (!_competitionManager.TryGetByCode(code, out var comp) || comp == null)
        {
            return NotFound(new { error = $"Competition '{code}' not found." });
        }

        return Ok(new
        {
            id = comp.Id,
            code = comp.Code,
            name = comp.Name,
            trackId = comp.TrackId,
            status = comp.Status.ToString(),
            rules = comp.Rules,
            participants = comp.Participants.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                registeredAt = p.RegisteredAt
            })
        });
    }

    [HttpPost("{code}/join")]
    public IActionResult Join(string code, [FromBody] JoinCompetitionRequest request)
    {
        try
        {
            var result = _competitionManager.Join(code, request.AgentName);
            return Ok(new
            {
                competitionId = result.CompetitionId,
                participantId = result.ParticipantId,
                agentName = result.AgentName,
                sessionToken = result.SessionToken
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Competition '{code}' not found." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{code}/start")]
    public IActionResult Start(string code)
    {
        if (!_competitionManager.TryGetByCode(code, out var comp) || comp == null)
        {
            return NotFound(new { error = $"Competition '{code}' not found." });
        }

        try
        {
            comp.Start();

            // 백그라운드에서 실시간 시뮬레이션 및 60Hz/15Hz 중계 루프 기동
            _ = Task.Run(async () =>
            {
                try
                {
                    await _raceOrchestrator.RunRaceAsync(
                        comp,
                        onTickBroadcast: (race, ct) => _spectatorBroadcaster.BroadcastTickAsync(race, ct)
                    );
                }
                catch (Exception)
                {
                    // 로깅 및 상태 복구
                }
            });

            return Ok(new
            {
                status = comp.Status.ToString(),
                message = "Race started successfully."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{code}/race")]
    public IActionResult GetActiveRace(string code)
    {
        if (!_competitionManager.TryGetByCode(code, out var comp) || comp == null)
        {
            return NotFound(new { error = $"Competition '{code}' not found." });
        }

        var race = _raceOrchestrator.GetActiveRace(comp.Id);
        if (race == null)
        {
            return Ok(new { isRunning = false, status = comp.Status.ToString() });
        }

        return Ok(new
        {
            isRunning = true,
            raceId = race.Id,
            currentTick = race.CurrentTick,
            status = race.Status.ToString(),
            drivers = race.Drivers.Select(d => new
            {
                participantId = d.ParticipantId,
                name = d.Name,
                lap = d.Progress.CurrentLap,
                speed = d.Car.Speed,
                isFinished = d.Progress.IsFinished,
                isDisqualified = d.Progress.IsDisqualified
            })
        });
    }

    [HttpGet("{code}/leaderboard")]
    public async Task<IActionResult> GetLeaderboard(string code)
    {
        if (!_competitionManager.TryGetByCode(code, out var comp) || comp == null)
        {
            return NotFound(new { error = $"Competition '{code}' not found." });
        }

        var results = await _resultRepository.GetResultsByCompetitionAsync(comp.Id);
        var entries = LeaderboardCalculator.Calculate(comp.Participants, results);

        return Ok(entries);
    }
}
