using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Racing.Application.Connections;
using Racing.Application.Orchestration;
using Racing.Domain.Cars;
using Racing.Domain.Geometry;
using Racing.Domain.Tracks;
using Racing.Simulation.Races;
using Racing.Simulation.Tracks;
using Xunit;
using Action = Racing.Contracts.V1.Action;
using ServerMessage = Racing.Contracts.V1.ServerMessage;

namespace Racing.Application.Tests;

public class ActionCollectorTests
{
    private sealed class MockAgentConnection : IAgentConnection
    {
        public Guid ParticipantId { get; }
        public string AgentName { get; }
        public bool IsConnected => true;
        public bool ShouldTimeout { get; set; } = false;

        public MockAgentConnection(Guid id, string name)
        {
            ParticipantId = id;
            AgentName = name;
        }

        public ValueTask SendAsync(ServerMessage message, CancellationToken ct = default) => ValueTask.CompletedTask;

        public async Task<DriverAction?> WaitForActionAsync(ulong tick, TimeSpan timeout, CancellationToken ct = default)
        {
            if (ShouldTimeout)
            {
                await Task.Delay(timeout + TimeSpan.FromMilliseconds(50), ct);
                return null;
            }

            return new DriverAction(0.5f, 1.0f, 0.0f);
        }

        public void SubmitAction(Action action) { }
        public void Close() { }
    }

    private static Race CreateDummyRace(Driver driver)
    {
        var trackDef = new TrackDefinition
        {
            Id = "test",
            Name = "Test",
            Points = new List<TrackPoint>
            {
                new(new Vector2(0f, 0f), 10f),
                new(new Vector2(100f, 0f), 10f),
                new(new Vector2(100f, 100f), 10f)
            }
        };
        var track = new SplineTrackBuilder(targetSampleStep: 1f).Build(trackDef);
        return new Race(Guid.NewGuid(), track, new List<Driver> { driver });
    }

    [Fact]
    public async Task CollectActionsAsync_ShouldReturnAction_WhenAgentRespondsInTime()
    {
        var driverId = Guid.NewGuid();
        var driver = new Driver(driverId, "FastAI", new Car(Vector2.Zero, 0f));
        var race = CreateDummyRace(driver);

        var conn = new MockAgentConnection(driverId, "FastAI");
        var connections = new Dictionary<Guid, IAgentConnection> { [driverId] = conn };

        var collector = new ActionCollector(TimeSpan.FromMilliseconds(50), maxConsecutiveTimeouts: 3);
        var actions = await collector.CollectActionsAsync(race, connections);

        Assert.True(actions.ContainsKey(driverId));
        Assert.Equal(0.5f, actions[driverId].Steering);
        Assert.Equal(1.0f, actions[driverId].Throttle);
    }

    [Fact]
    public async Task CollectActionsAsync_ShouldDisqualify_AfterMaxTimeouts()
    {
        var driverId = Guid.NewGuid();
        var driver = new Driver(driverId, "SlowAI", new Car(Vector2.Zero, 0f));
        var race = CreateDummyRace(driver);

        var conn = new MockAgentConnection(driverId, "SlowAI") { ShouldTimeout = true };
        var connections = new Dictionary<Guid, IAgentConnection> { [driverId] = conn };

        var collector = new ActionCollector(TimeSpan.FromMilliseconds(20), maxConsecutiveTimeouts: 3);

        // 1회 타임아웃
        await collector.CollectActionsAsync(race, connections);
        Assert.False(driver.Progress.IsDisqualified);

        // 2회 타임아웃
        await collector.CollectActionsAsync(race, connections);
        Assert.False(driver.Progress.IsDisqualified);

        // 3회 타임아웃 -> 실격
        await collector.CollectActionsAsync(race, connections);
        Assert.True(driver.Progress.IsDisqualified);
        Assert.Contains("Disqualified", driver.Progress.DnfReason);
    }
}
