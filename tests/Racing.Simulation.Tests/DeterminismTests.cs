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

public class DeterminismTests
{
    [Fact]
    public void Simulation_GivenIdenticalActions_MustBe100PercentDeterministic()
    {
        // 1. 공통 트랙 생성
        var points = new List<TrackPoint>();
        const int numPoints = 8;
        const float radius = 50f;
        for (int i = 0; i < numPoints; i++)
        {
            float theta = (MathF.Tau / numPoints) * i;
            points.Add(new TrackPoint(new Vector2(MathF.Cos(theta) * radius, MathF.Sin(theta) * radius), 12f));
        }

        var trackDef = new TrackDefinition
        {
            Id = "determinism-circle",
            Name = "Determinism Circle",
            IsLoop = true,
            DefaultLaps = 2,
            Points = points
        };

        var track = new SplineTrackBuilder(targetSampleStep: 1f).Build(trackDef);

        // 2. 미리 고정된 1200 ticks(20초) 분량의 액션 시퀀스 생성 (고정 시드 의사난수)
        const int totalTicks = 1200;
        var fixedRandom = new Random(12345);
        var preRecordedActions = new DriverAction[totalTicks];
        for (int i = 0; i < totalTicks; i++)
        {
            float steering = (float)(fixedRandom.NextDouble() * 2.0 - 1.0) * 0.4f;
            float throttle = (float)(fixedRandom.NextDouble() * 0.8 + 0.2);
            float brake = fixedRandom.NextDouble() < 0.1 ? 0.5f : 0f;
            preRecordedActions[i] = new DriverAction(steering, throttle, brake);
        }

        // 3. 런 1 실행
        var result1 = RunSimulation(track, preRecordedActions);

        // 4. 런 2 실행
        var result2 = RunSimulation(track, preRecordedActions);

        // 5. 비트 단위 일치 검증
        Assert.Equal(result1.Car.Position.X, result2.Car.Position.X);
        Assert.Equal(result1.Car.Position.Y, result2.Car.Position.Y);
        Assert.Equal(result1.Car.Velocity.X, result2.Car.Velocity.X);
        Assert.Equal(result1.Car.Velocity.Y, result2.Car.Velocity.Y);
        Assert.Equal(result1.Car.Rotation, result2.Car.Rotation);
        Assert.Equal(result1.Car.AngularVelocity, result2.Car.AngularVelocity);
        Assert.Equal(result1.Progress.CurrentLap, result2.Progress.CurrentLap);
        Assert.Equal(result1.Progress.NextCheckpointIndex, result2.Progress.NextCheckpointIndex);
        Assert.Equal(result1.Progress.TotalRaceTimeSeconds, result2.Progress.TotalRaceTimeSeconds);
        Assert.Equal(result1.Progress.IsFinished, result2.Progress.IsFinished);
    }

    private static Driver RunSimulation(TrackRuntime track, DriverAction[] actions)
    {
        var startPos = track.Centerline[0];
        var startTangent = track.Tangents[0];
        float startRot = MathF.Atan2(startTangent.Y, startTangent.X);

        var car = new Car(startPos, startRot);
        var driverId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var driver = new Driver(driverId, "DeterministicDriver", car);

        var race = new Race(Guid.NewGuid(), track, new List<Driver> { driver }, totalLaps: 2);
        race.Start();

        var simulator = new RaceSimulator(track, totalLaps: 2);

        for (int tick = 0; tick < actions.Length; tick++)
        {
            var actionDict = new Dictionary<Guid, DriverAction>
            {
                [driverId] = actions[tick]
            };

            simulator.Step(race, actionDict);
        }

        return driver;
    }
}
