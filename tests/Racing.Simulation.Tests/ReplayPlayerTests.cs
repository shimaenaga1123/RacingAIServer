using Racing.Domain.Cars;
using Racing.Domain.Geometry;
using Racing.Domain.Tracks;
using Racing.Simulation.Races;
using Racing.Simulation.Replay;
using Racing.Simulation.Tracks;

namespace Racing.Simulation.Tests
{
    public class ReplayPlayerTests
    {
        [Fact]
        public void ReplayShouldProduceExactSameRaceResultAsLive()
        {
            // 1. 트랙 생성
            List<TrackPoint> points = [];
            const int numPoints = 8;
            const float radius = 40f;
            for (int i = 0; i < numPoints; i++)
            {
                float theta = MathF.Tau / numPoints * i;
                points.Add(new TrackPoint(new Vector2(MathF.Cos(theta) * radius, MathF.Sin(theta) * radius), 12f));
            }

            TrackDefinition trackDef = new()
            {
                Id = "replay-loop",
                Name = "Replay Loop",
                IsLoop = true,
                DefaultLaps = 2,
                Points = points
            };

            var track = new SplineTrackBuilder(targetSampleStep: 1f).Build(trackDef);

            // 2. 라이브 경기 준비 (2대 참가)
            var startPos = track.Centerline[0];
            var tangent = track.Tangents[0];
            float rot = MathF.Atan2(tangent.Y, tangent.X);

            Guid p1 = Guid.NewGuid();
            Guid p2 = Guid.NewGuid();

            Driver driver1 = new(p1, "AI_Alpha", new Car(startPos, rot));
            Driver driver2 = new(p2, "AI_Beta", new Car(startPos + (track.Normals[0] * 3f), rot));

            Race liveRace = new(Guid.NewGuid(), track, [driver1, driver2], totalLaps: 2);
            liveRace.Start();

            RaceSimulator simulator = new(track, totalLaps: 2);
            ReplayRecorder recorder = new(liveRace, seed: 100);

            // 3. 라이브 경기 600 ticks 실행 및 녹화
            Random rnd = new(777);
            for (ulong tick = 0; tick < 600; tick++)
            {
                Dictionary<Guid, DriverAction> actions = new()
                {
                    [p1] = new((float)((rnd.NextDouble() * 0.4) - 0.2), 1f, 0f),
                    [p2] = new((float)((rnd.NextDouble() * 0.4) - 0.2), 0.9f, 0f)
                };

                recorder.RecordTick(tick, actions);
                simulator.Step(liveRace, actions);
            }

            var replayData = recorder.Export();

            // 4. 리플레이 플레이어로 재현 실행
            ReplayPlayer player = new(replayData, track);
            player.StepAll();

            // 5. 라이브 상태 vs 리플레이 상태 비트 단위 검증
            Assert.Equal(600, player.TotalFrames);
            Assert.True(player.IsFinished);

            var replayDriver1 = player.Race.Drivers[0];
            var replayDriver2 = player.Race.Drivers[1];

            // Driver 1 검증
            Assert.Equal(driver1.Car.Position.X, replayDriver1.Car.Position.X);
            Assert.Equal(driver1.Car.Position.Y, replayDriver1.Car.Position.Y);
            Assert.Equal(driver1.Car.Velocity.X, replayDriver1.Car.Velocity.X);
            Assert.Equal(driver1.Car.Velocity.Y, replayDriver1.Car.Velocity.Y);
            Assert.Equal(driver1.Progress.CurrentLap, replayDriver1.Progress.CurrentLap);
            Assert.Equal(driver1.Progress.NextCheckpointIndex, replayDriver1.Progress.NextCheckpointIndex);

            // Driver 2 검증
            Assert.Equal(driver2.Car.Position.X, replayDriver2.Car.Position.X);
            Assert.Equal(driver2.Car.Position.Y, replayDriver2.Car.Position.Y);
            Assert.Equal(driver2.Car.Velocity.X, replayDriver2.Car.Velocity.X);
            Assert.Equal(driver2.Car.Velocity.Y, replayDriver2.Car.Velocity.Y);
        }
    }
}
