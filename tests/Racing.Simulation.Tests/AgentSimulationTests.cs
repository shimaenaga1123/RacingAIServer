using Racing.Domain.Cars;
using Racing.Domain.Geometry;
using Racing.Domain.Tracks;
using Racing.Simulation.Agents;
using Racing.Simulation.Observations;
using Racing.Simulation.Races;
using Racing.Simulation.Tracks;

namespace Racing.Simulation.Tests
{
    public class AgentSimulationTests
    {
        [Fact]
        public void FollowTrackAgentShouldCompleteLapsSuccessfully()
        {
            // 타원형 서킷 정의
            List<TrackPoint> points = [];
            const int numPoints = 12;
            const float radiusX = 60f;
            const float radiusY = 30f;

            for (int i = 0; i < numPoints; i++)
            {
                float theta = MathF.Tau / numPoints * i;
                Vector2 pos = new(MathF.Cos(theta) * radiusX, MathF.Sin(theta) * radiusY);
                points.Add(new TrackPoint(pos, 12f));
            }

            TrackDefinition trackDef = new()
            {
                Id = "oval",
                Name = "Oval Track",
                IsLoop = true,
                DefaultLaps = 2,
                Points = points
            };

            var track = new SplineTrackBuilder(targetSampleStep: 1f, checkpointCount: 8).Build(trackDef);
            TrackCoordinateConverter converter = new(track);
            ObservationBuilder obsBuilder = new(converter);

            // 시작 위치 및 회전 설정
            var startPos = track.Centerline[0];
            var startTangent = track.Tangents[0];
            float startRot = MathF.Atan2(startTangent.Y, startTangent.X);

            Car car = new(startPos, startRot);
            Guid driverId = Guid.NewGuid();
            Driver driver = new(driverId, "FollowTrackBot", car);

            Race race = new(Guid.NewGuid(), track, [driver], totalLaps: 2);
            race.Start();

            RaceSimulator simulator = new(track, totalLaps: 2);
            FollowTrackAgent agent = new();

            // 최대 3000 틱 (50초 분량) 시뮬레이션
            for (int tick = 0; tick < 3000; tick++)
            {
                if (race.Status == RaceStatus.Finished)
                {
                    break;
                }

                var obs = obsBuilder.Build(driver, race);
                var action = agent.Act(obs);

                Dictionary<Guid, DriverAction> actionDict = new()
                {
                    [driverId] = action
                };

                simulator.Step(race, actionDict);
            }

            Assert.Equal(RaceStatus.Finished, race.Status);
            Assert.True(driver.Progress.IsFinished);
            Assert.Equal(2, driver.Progress.LapTimes.Count);
            Assert.True(driver.Progress.BestLapTimeSeconds > 0f);
        }
    }
}
