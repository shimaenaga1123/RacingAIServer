using System;
using System.Collections.Generic;
using Racing.Domain.Cars;
using Racing.Domain.Geometry;
using Racing.Domain.Tracks;
using Racing.Simulation.Agents;
using Racing.Simulation.Observations;
using Racing.Simulation.Races;
using Racing.Simulation.Tracks;
using Xunit;

namespace Racing.Simulation.Tests;

public class AgentSimulationTests
{
    [Fact]
    public void FollowTrackAgent_ShouldCompleteLapsSuccessfully()
    {
        // 타원형 서킷 정의
        var points = new List<TrackPoint>();
        const int numPoints = 12;
        const float radiusX = 60f;
        const float radiusY = 30f;

        for (int i = 0; i < numPoints; i++)
        {
            float theta = (MathF.Tau / numPoints) * i;
            var pos = new Vector2(MathF.Cos(theta) * radiusX, MathF.Sin(theta) * radiusY);
            points.Add(new TrackPoint(pos, 12f));
        }

        var trackDef = new TrackDefinition
        {
            Id = "oval",
            Name = "Oval Track",
            IsLoop = true,
            DefaultLaps = 2,
            Points = points
        };

        var track = new SplineTrackBuilder(targetSampleStep: 1f, checkpointCount: 8).Build(trackDef);
        var converter = new TrackCoordinateConverter(track);
        var obsBuilder = new ObservationBuilder(converter);

        // 시작 위치 및 회전 설정
        var startPos = track.Centerline[0];
        var startTangent = track.Tangents[0];
        float startRot = MathF.Atan2(startTangent.Y, startTangent.X);

        var car = new Car(startPos, startRot);
        var driverId = Guid.NewGuid();
        var driver = new Driver(driverId, "FollowTrackBot", car);

        var race = new Race(Guid.NewGuid(), track, new List<Driver> { driver }, totalLaps: 2);
        race.Start();

        var simulator = new RaceSimulator(track, totalLaps: 2);
        var agent = new FollowTrackAgent();

        // 최대 3000 틱 (50초 분량) 시뮬레이션
        for (int tick = 0; tick < 3000; tick++)
        {
            if (race.Status == RaceStatus.Finished) break;

            var obs = obsBuilder.Build(driver, race);
            var action = agent.Act(obs);

            var actionDict = new Dictionary<Guid, DriverAction>
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
