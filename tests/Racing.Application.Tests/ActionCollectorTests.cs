using Racing.Application.Connections;
using Racing.Application.Orchestration;
using Racing.Domain.Cars;
using Racing.Domain.Geometry;
using Racing.Domain.Tracks;
using Racing.Simulation.Races;
using Racing.Simulation.Tracks;
using Action = Racing.Contracts.V1.Action;
using ServerMessage = Racing.Contracts.V1.ServerMessage;

namespace Racing.Application.Tests
{
    public class ActionCollectorTests
    {
        private sealed class MockAgentConnection(Guid id, string name) : IAgentConnection
        {
            public Guid ParticipantId { get; } = id;
            public string AgentName { get; } = name;
            public bool IsConnected => true;
            public bool ShouldTimeout { get; set; }

            public ValueTask SendAsync(ServerMessage message, CancellationToken ct = default)
            {
                return ValueTask.CompletedTask;
            }

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
            TrackDefinition trackDef = new()
            {
                Id = "test",
                Name = "Test",
                Points =
                [
                    new(new Vector2(0f, 0f), 10f),
                    new(new Vector2(100f, 0f), 10f),
                    new(new Vector2(100f, 100f), 10f)
                ]
            };
            var track = new SplineTrackBuilder(targetSampleStep: 1f).Build(trackDef);
            return new Race(Guid.NewGuid(), track, [driver]);
        }

        [Fact]
        public async Task CollectActionsAsyncShouldReturnActionWhenAgentRespondsInTime()
        {
            Guid driverId = Guid.NewGuid();
            Driver driver = new(driverId, "FastAI", new Car(Vector2.Zero, 0f));
            var race = CreateDummyRace(driver);

            MockAgentConnection conn = new(driverId, "FastAI");
            Dictionary<Guid, IAgentConnection> connections = new() { [driverId] = conn };

            ActionCollector collector = new(TimeSpan.FromMilliseconds(50), maxConsecutiveTimeouts: 3);
            var actions = await collector.CollectActionsAsync(race, connections);

            Assert.True(actions.ContainsKey(driverId));
            Assert.Equal(0.5f, actions[driverId].Steering);
            Assert.Equal(1.0f, actions[driverId].Throttle);
        }

        [Fact]
        public async Task CollectActionsAsyncShouldDisqualifyAfterMaxTimeouts()
        {
            Guid driverId = Guid.NewGuid();
            Driver driver = new(driverId, "SlowAI", new Car(Vector2.Zero, 0f));
            var race = CreateDummyRace(driver);

            MockAgentConnection conn = new(driverId, "SlowAI") { ShouldTimeout = true };
            Dictionary<Guid, IAgentConnection> connections = new() { [driverId] = conn };

            ActionCollector collector = new(TimeSpan.FromMilliseconds(20), maxConsecutiveTimeouts: 3);

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
}
