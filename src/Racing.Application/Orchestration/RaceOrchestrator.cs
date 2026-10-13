using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Racing.Application.Connections;
using Racing.Application.Persistence;
using Racing.Contracts.V1;
using Racing.Domain.Cars;
using Racing.Domain.Competitions;
using Racing.Domain.Geometry;
using Racing.Simulation;
using Racing.Simulation.Observations;
using Racing.Simulation.Races;
using Racing.Simulation.Replay;
using Racing.Simulation.Tracks;

namespace Racing.Application.Orchestration;

public sealed class RaceOrchestrator(
    ITrackLoader trackLoader,
    AgentConnectionRegistry connectionRegistry,
    IRaceResultRepository resultRepository,
    IReplayRepository replayRepository,
    IScoringPolicy? scoringPolicy = null,
    ILogger<RaceOrchestrator>? logger = null) : IRaceOrchestrator
{
    private readonly ITrackLoader _trackLoader = trackLoader;
    private readonly AgentConnectionRegistry _connectionRegistry = connectionRegistry;
    private readonly IRaceResultRepository _resultRepository = resultRepository;
    private readonly IReplayRepository _replayRepository = replayRepository;
    private readonly IScoringPolicy _scoringPolicy = scoringPolicy ?? new StandardScoringPolicy();
    private readonly ILogger<RaceOrchestrator> _logger = logger ?? NullLogger<RaceOrchestrator>.Instance;

    private readonly ConcurrentDictionary<Guid, Race> _activeRaces = new();

    public Race? GetActiveRace(Guid competitionId)
    {
        _activeRaces.TryGetValue(competitionId, out var race);
        return race;
    }

    public async Task<Race> RunRaceAsync(
        Competition competition,
        Func<Race, CancellationToken, ValueTask>? onTickBroadcast = null,
        CancellationToken ct = default)
    {
        var rules = competition.Rules;
        var trackDef = _trackLoader.LoadDefinition(competition.TrackId);
        var trackRuntime = _trackLoader.LoadRuntime(competition.TrackId);

        Guid raceId = Guid.NewGuid();
        _logger.LogInformation("Starting race {RaceId} for competition '{Name}' ({Code})", raceId, competition.Name, competition.Code);

        // 1. 출발 그리드 배치 및 Driver 생성
        var drivers = BuildStartingGrid(competition.Participants, trackRuntime);
        var race = new Race(raceId, trackRuntime, drivers, rules.TotalLaps);
        _activeRaces[competition.Id] = race;

        var simulator = new RaceSimulator(trackRuntime, totalLaps: rules.TotalLaps);
        var coordinateConverter = new TrackCoordinateConverter(trackRuntime);
        var observationBuilder = new ObservationBuilder(coordinateConverter);
        var actionCollector = new ActionCollector(
            timeout: TimeSpan.FromMilliseconds(rules.ActionTimeoutMs),
            maxConsecutiveTimeouts: rules.MaxConsecutiveTimeouts
        );
        var replayRecorder = new ReplayRecorder(race, seed: 42);

        // 2. 연결된 Agent Connection 수집 및 레이스 할당 메시지 발송
        var connections = new Dictionary<Guid, IAgentConnection>();
        foreach (var driver in drivers)
        {
            if (_connectionRegistry.TryGet(driver.ParticipantId, out var conn) && conn != null && conn.IsConnected)
            {
                connections[driver.ParticipantId] = conn;

                // RaceAssigned 발송
                var assignMsg = ProtobufMapper.ToRaceAssignedMessage(raceId, trackDef, trackRuntime, rules);
                await conn.SendAsync(assignMsg, ct);

                // RaceStarted 발송
                var startMsg = new ServerMessage
                {
                    RaceStarted = new RaceStarted
                    {
                        RaceId = raceId.ToString(),
                        StartTick = 0,
                        TotalLaps = (uint)rules.TotalLaps
                    }
                };
                await conn.SendAsync(startMsg, ct);
            }
        }

        // 레이스 시작
        race.Start();

        // 3. 60Hz 틱 시뮬레이션 루프
        float tickIntervalMs = 1000f / Math.Max(10, rules.TickRate);
        const int maxTicks = 60 * 120; // 최대 2분 (7200 ticks) 세이프가드
        var sw = new Stopwatch();

        try
        {
            while (race.Status == RaceStatus.Running && race.CurrentTick < (ulong)maxTicks && !ct.IsCancellationRequested)
            {
                sw.Restart();

                // 3-1. 각 에이전트에게 현재 틱 Observation 전송
                var obsTasks = new List<Task>(race.Drivers.Count);
                foreach (var driver in race.Drivers)
                {
                    if (connections.TryGetValue(driver.ParticipantId, out var conn) && conn.IsConnected)
                    {
                        var obs = observationBuilder.Build(driver, race);
                        var obsMsg = ProtobufMapper.ToObservationMessage(obs);
                        obsTasks.Add(conn.SendAsync(obsMsg, ct).AsTask());
                    }
                }
                await Task.WhenAll(obsTasks);

                // 3-2. Action 병렬 수집
                var actions = await actionCollector.CollectActionsAsync(race, connections, ct);

                // 3-3. 물리 및 룰 시뮬레이션 Step
                simulator.Step(race, actions);

                // 3-4. Replay 프레임 기록
                replayRecorder.RecordTick(race.CurrentTick, actions);

                // 3-5. 관전자 실시간 브로드캐스트 (15Hz 압축)
                if (onTickBroadcast != null)
                {
                    await onTickBroadcast(race, ct);
                }

                // 3-6. 틱 주기 정밀 딜레이
                sw.Stop();
                int elapsedMs = (int)sw.ElapsedMilliseconds;
                int delayMs = (int)tickIntervalMs - elapsedMs;
                if (delayMs > 0)
                {
                    try
                    {
                        await Task.Delay(delayMs, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Race {RaceId} execution was cancelled.", raceId);
        }
        finally
        {
            if (race.Status == RaceStatus.Running)
            {
                race.Finish();
            }
            _activeRaces.TryRemove(competition.Id, out _);
        }

        // 4. 순위 계산 및 결과 생성
        var rankedDrivers = CalculateRankings(race.Drivers, trackRuntime);
        var persistedResults = new List<PersistedRaceResult>(rankedDrivers.Count);

        for (int rank = 1; rank <= rankedDrivers.Count; rank++)
        {
            var driver = rankedDrivers[rank - 1];
            bool isDnf = driver.Progress.IsDisqualified || (!driver.Progress.IsFinished);
            int points = _scoringPolicy.CalculatePoints(rank, isDnf);

            float fastestLap = driver.Progress.BestLapTimeSeconds == float.MaxValue
                ? 0f
                : driver.Progress.BestLapTimeSeconds;

            persistedResults.Add(new PersistedRaceResult(
                RaceId: raceId,
                CompetitionId: competition.Id,
                ParticipantId: driver.ParticipantId,
                ParticipantName: driver.Name,
                Rank: rank,
                TotalTimeSeconds: driver.Progress.TotalRaceTimeSeconds,
                FastestLapSeconds: fastestLap,
                PointsAwarded: points,
                IsDnf: isDnf,
                DnfReason: driver.Progress.DnfReason,
                FinishedAt: DateTimeOffset.UtcNow
            ));

            // 개별 에이전트에 RaceFinished 알림
            if (connections.TryGetValue(driver.ParticipantId, out var conn) && conn.IsConnected)
            {
                var finishMsg = new ServerMessage
                {
                    RaceFinished = new RaceFinished
                    {
                        RaceId = raceId.ToString(),
                        Rank = (uint)rank,
                        TotalTimeSeconds = driver.Progress.TotalRaceTimeSeconds,
                        FastestLapSeconds = fastestLap,
                        IsDnf = isDnf,
                        DnfReason = driver.Progress.DnfReason ?? string.Empty
                    }
                };
                await conn.SendAsync(finishMsg, CancellationToken.None);

                var compFinishedMsg = new ServerMessage
                {
                    CompetitionFinished = new CompetitionFinished
                    {
                        CompetitionId = competition.Id.ToString(),
                        FinalRank = (uint)rank,
                        TotalPoints = points
                    }
                };
                await conn.SendAsync(compFinishedMsg, CancellationToken.None);
            }
        }

        // 5. 결과 및 Replay 영속 저장
        try
        {
            await _resultRepository.SaveResultsAsync(persistedResults, CancellationToken.None);
            var replayData = replayRecorder.Export();
            await _replayRepository.SaveReplayAsync(replayData, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist race results or replay for race {RaceId}", raceId);
        }

        // 대회 상태 완료
        competition.Finish();
        _logger.LogInformation("Race {RaceId} finished successfully. Total drivers: {Count}", raceId, rankedDrivers.Count);

        return race;
    }

    private static List<Driver> BuildStartingGrid(IReadOnlyList<Participant> participants, TrackRuntime track)
    {
        var drivers = new List<Driver>(participants.Count);
        var startCenter = track.Centerline[0];
        var tangent = track.Tangents[0];
        var normal = track.Normals[0];
        float heading = MathF.Atan2(tangent.Y, tangent.X);

        for (int i = 0; i < participants.Count; i++)
        {
            var p = participants[i];
            // 엇갈린 스타트 그리드 배치: 결승선 뒤 10m부터 6m 간격, 좌우(-2m, +2m)
            float setback = 10f + (i * 6f);
            float lateralOffset = (i % 2 == 0) ? -2.2f : 2.2f;

            Vector2 gridPosition = startCenter - (tangent * setback) + (normal * lateralOffset);
            var car = new Car(gridPosition, heading);
            drivers.Add(new Driver(p.Id, p.Name, car));
        }

        return drivers;
    }

    private static List<Driver> CalculateRankings(IReadOnlyList<Driver> drivers, TrackRuntime track)
    {
        return drivers.OrderBy(d =>
        {
            if (d.Progress.IsDisqualified) return 2; // 실격은 최하위
            if (d.Progress.IsFinished) return 0;    // 완주자는 최상위
            return 1;                               // 미완주자(시간초과)는 중간
        })
        .ThenBy(d =>
        {
            // 완주자는 총 레이스 시간 오름차순
            if (d.Progress.IsFinished) return d.Progress.TotalRaceTimeSeconds;
            // 미완주자는 진행한 랩 역순
            return -d.Progress.CurrentLap;
        })
        .ThenByDescending(d => d.TrackPosition.DistanceAlongTrack) // 동일 랩 내 주행 거리 내림차순
        .ToList();
    }
}
