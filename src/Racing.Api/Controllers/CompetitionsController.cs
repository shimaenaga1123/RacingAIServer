using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Racing.Application.Competitions;
using Racing.Domain.Competitions;

namespace Racing.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class CompetitionsController : ControllerBase
{
    private readonly CompetitionManager _competitionManager;

    public CompetitionsController(CompetitionManager competitionManager)
    {
        _competitionManager = competitionManager;
    }

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
            return Ok(new { status = comp.Status.ToString() });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
