using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Racing.Application.Connections;
using Racing.Application.Orchestration;
using Racing.Application.Persistence;
using Racing.Contracts.V1;
using Racing.Domain.Cars;
using Racing.Domain.Competitions;
using Racing.Simulation.Replay;
using Racing.Simulation.Tracks;
using Xunit;
using Action = Racing.Contracts.V1.Action;

namespace Racing.Application.Tests;

public class RaceOrchestratorTests
{
    private sealed class MockLocalConnection(Guid participantId, string name, DriverAction actionToReturn) : IAgentConnection
    {
        public Guid ParticipantId { get; } = participantId;
        public string AgentName { get; } = name;
        public bool IsConnected { get; private set; } = true;
        public List<ServerMessage> ReceivedMessages { get; } = [];

        public ValueTask SendAsync(ServerMessage message, CancellationToken ct = default)
        {
            ReceivedMessages.Add(message);
            return ValueTask.CompletedTask;
        }

        public Task<DriverAction?> WaitForActionAsync(ulong tick, TimeSpan timeout, CancellationToken ct = default)
        {
            return Task.FromResult<DriverAction?>(actionToReturn);
        }

        public void SubmitAction(Action action) { }

        public void Close()
        {
            IsConnected = false;
        }
    }

    private sealed class InMemoryResultRepository : IRaceResultRepository
    {
        public List<PersistedRaceResult> SavedResults { get; } = [];

        public Task SaveResultsAsync(IEnumerable<PersistedRaceResult> results, CancellationToken ct = default)
        {
            SavedResults.AddRange(results);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PersistedRaceResult>> GetResultsByCompetitionAsync(Guid competitionId, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<PersistedRaceResult>>(SavedResults.FindAll(r => r.CompetitionId == competitionId));
        }

        public Task<IReadOnlyList<PersistedRaceResult>> GetResultsByRaceAsync(Guid raceId, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<PersistedRaceResult>>(SavedResults.FindAll(r => r.RaceId == raceId));
        }
    }

    private sealed class InMemoryReplayRepository : IReplayRepository
    {
        public ReplayData? SavedReplay { get; private set; }

        public Task SaveReplayAsync(ReplayData replay, CancellationToken ct = default)
        {
            SavedReplay = replay;
            return Task.CompletedTask;
        }

        public Task<ReplayData?> GetReplayAsync(Guid raceId, CancellationToken ct = default)
        {
            return Task.FromResult(SavedReplay != null && SavedReplay.Header.RaceId == raceId ? SavedReplay : null);
        }
    }

    [Fact]
    public async Task RunRaceAsyncShouldExecuteSimulationLoopAndSaveResultsAndReplay()
    {
        var trackLoader = new TrackLoader();
        var registry = new AgentConnectionRegistry();
        var resultRepo = new InMemoryResultRepository();
        var replayRepo = new InMemoryReplayRepository();

        var orchestrator = new RaceOrchestrator(
            trackLoader: trackLoader,
            connectionRegistry: registry,
            resultRepository: resultRepo,
            replayRepository: replayRepo,
            scoringPolicy: new StandardScoringPolicy()
        );

        var comp = new Competition(
            id: Guid.NewGuid(),
            name: "Orchestration Test Cup",
            code: "999888",
            trackId: "beginner-circuit",
            rules: new CompetitionRules
            {
                TotalLaps = 1,
                TickRate = 60,
                ActionTimeoutMs = 10,
                MaxConsecutiveTimeouts = 5
            }
        );
        comp.Open();

        var p1 = comp.AddParticipant("FastBot");
        var p2 = comp.AddParticipant("SlowBot");

        // p1: 전진 스로틀, p2: 정지
        var conn1 = new MockLocalConnection(p1.Id, p1.Name, new DriverAction(0f, 1f, 0f));
        var conn2 = new MockLocalConnection(p2.Id, p2.Name, new DriverAction(0f, 0.2f, 0f));

        registry.Register(conn1);
        registry.Register(conn2);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        // 레이스 실행 (테스트 속도를 위해 150틱 후 취소)
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            cts.Cancel();
        });

        var race = await orchestrator.RunRaceAsync(comp, ct: cts.Token);

        // 검증
        Assert.NotNull(race);
        Assert.Equal(CompetitionStatus.Finished, comp.Status);

        // 결과 저장 확인
        Assert.Equal(2, resultRepo.SavedResults.Count);
        Assert.Equal(1, resultRepo.SavedResults[0].Rank);
        Assert.Equal(2, resultRepo.SavedResults[1].Rank);

        // Replay 저장 확인
        Assert.NotNull(replayRepo.SavedReplay);
        Assert.True(replayRepo.SavedReplay.Frames.Count > 0);

        // 에이전트 수신 메시지 검증 (RaceAssigned, RaceStarted, Observation, RaceFinished 등)
        Assert.Contains(conn1.ReceivedMessages, m => m.PayloadCase == ServerMessage.PayloadOneofCase.RaceAssigned);
        Assert.Contains(conn1.ReceivedMessages, m => m.PayloadCase == ServerMessage.PayloadOneofCase.RaceStarted);
        Assert.Contains(conn1.ReceivedMessages, m => m.PayloadCase == ServerMessage.PayloadOneofCase.Observation);
        Assert.Contains(conn1.ReceivedMessages, m => m.PayloadCase == ServerMessage.PayloadOneofCase.RaceFinished);
        Assert.Contains(conn1.ReceivedMessages, m => m.PayloadCase == ServerMessage.PayloadOneofCase.CompetitionFinished);
    }
}
