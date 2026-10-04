using System;
using System.Collections.Generic;
using Racing.Simulation.Races;
using Racing.Simulation.Tracks;

namespace Racing.Simulation.Observations;

public sealed class ObservationBuilder
{
    private static readonly float[] DefaultLookaheadDistances = [5f, 10f, 20f, 40f];
    private readonly TrackCoordinateConverter _coordinateConverter;

    public ObservationBuilder(TrackCoordinateConverter coordinateConverter)
    {
        _coordinateConverter = coordinateConverter;
    }

    public Observation Build(Driver driver, Race race)
    {
        var car = driver.Car;
        var progress = driver.Progress;
        var trackPos = driver.TrackPosition;

        var player = new PlayerObservation(
            Position: car.Position,
            Velocity: car.Velocity,
            Speed: car.Speed,
            Rotation: car.Rotation,
            AngularVelocity: car.AngularVelocity,
            Steering: car.Steering,
            Throttle: car.Throttle,
            Brake: car.Brake
        );

        var curvatures = _coordinateConverter.GetAheadCurvatures(
            trackPos.DistanceAlongTrack,
            DefaultLookaheadDistances
        );

        var track = new TrackObservation(
            DistanceAlongTrack: trackPos.DistanceAlongTrack,
            LateralOffset: trackPos.LateralOffset,
            HeadingError: trackPos.HeadingError,
            TrackWidth: trackPos.TrackWidth,
            AheadCurvatures: curvatures
        );

        var lap = new LapObservation(
            CurrentLap: progress.CurrentLap,
            TotalLaps: race.TotalLaps,
            CurrentLapTime: progress.CurrentLapTimeSeconds,
            LastLapTime: progress.LastLapTimeSeconds,
            BestLapTime: progress.BestLapTimeSeconds == float.MaxValue ? 0f : progress.BestLapTimeSeconds,
            NextCheckpointIndex: progress.NextCheckpointIndex,
            TotalCheckpoints: race.Track.Checkpoints.Count
        );

        var opponents = new List<OpponentObservation>();
        foreach (var opp in race.Drivers)
        {
            if (opp.ParticipantId == driver.ParticipantId) continue;

            opponents.Add(new OpponentObservation(
                ParticipantId: opp.ParticipantId,
                Name: opp.Name,
                Position: opp.Car.Position,
                Velocity: opp.Car.Velocity,
                Speed: opp.Car.Speed,
                Rotation: opp.Car.Rotation,
                DistanceAlongTrack: opp.TrackPosition.DistanceAlongTrack,
                LateralOffset: opp.TrackPosition.LateralOffset
            ));
        }

        return new Observation(
            RaceId: race.Id,
            Tick: race.CurrentTick,
            TimestampMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Player: player,
            Track: track,
            Lap: lap,
            Opponents: opponents
        );
    }
}
