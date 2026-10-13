using System;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using Racing.Contracts.V1;
using Racing.Domain.Competitions;
using Racing.Domain.Tracks;
using Racing.Simulation.Observations;
using Racing.Simulation.Tracks;
using SimObservation = Racing.Simulation.Observations.Observation;
using Vector2Msg = Racing.Contracts.V1.Vector2Msg;

namespace Racing.Application.Orchestration;

public static class ProtobufMapper
{
    public static ServerMessage ToObservationMessage(SimObservation obs)
    {
        var msg = new ServerMessage
        {
            Observation = new Contracts.V1.Observation
            {
                RaceId = obs.RaceId.ToString(),
                Tick = obs.Tick,
                TimestampMs = obs.TimestampMs,
                Player = new CarTelemetry
                {
                    Position = new Vector2Msg { X = obs.Player.Position.X, Y = obs.Player.Position.Y },
                    Velocity = new Vector2Msg { X = obs.Player.Velocity.X, Y = obs.Player.Velocity.Y },
                    Speed = obs.Player.Speed,
                    Rotation = obs.Player.Rotation,
                    AngularVelocity = obs.Player.AngularVelocity,
                    CurrentSteering = obs.Player.Steering,
                    CurrentThrottle = obs.Player.Throttle,
                    CurrentBrake = obs.Player.Brake
                },
                Track = new TrackTelemetry
                {
                    DistanceAlongTrack = obs.Track.DistanceAlongTrack,
                    LateralOffset = obs.Track.LateralOffset,
                    HeadingError = obs.Track.HeadingError,
                    TrackWidth = obs.Track.TrackWidth
                },
                Lap = new LapTelemetry
                {
                    CurrentLap = (uint)obs.Lap.CurrentLap,
                    TotalLaps = (uint)obs.Lap.TotalLaps,
                    CurrentLapTime = obs.Lap.CurrentLapTime,
                    LastLapTime = obs.Lap.LastLapTime,
                    BestLapTime = obs.Lap.BestLapTime,
                    NextCheckpointIdx = (uint)obs.Lap.NextCheckpointIndex,
                    TotalCheckpoints = (uint)obs.Lap.TotalCheckpoints
                }
            }
        };

        if (obs.Track.AheadCurvatures != null)
        {
            msg.Observation.Track.FutureCurvatures.AddRange(obs.Track.AheadCurvatures);
        }

        foreach (var opp in obs.Opponents)
        {
            msg.Observation.Opponents.Add(new OpponentTelemetry
            {
                ParticipantId = opp.ParticipantId.ToString(),
                Name = opp.Name,
                Position = new Vector2Msg { X = opp.Position.X, Y = opp.Position.Y },
                Velocity = new Vector2Msg { X = opp.Velocity.X, Y = opp.Velocity.Y },
                Speed = opp.Speed,
                Rotation = opp.Rotation,
                DistanceAlongTrack = opp.DistanceAlongTrack,
                LateralOffset = opp.LateralOffset
            });
        }

        return msg;
    }

    public static ServerMessage ToRaceAssignedMessage(
        Guid raceId,
        TrackDefinition trackDef,
        TrackRuntime trackRuntime,
        CompetitionRules rules)
    {
        var trackMsg = new TrackDefinitionData
        {
            Id = trackDef.Id,
            Name = trackDef.Name,
            IsLoop = trackDef.IsLoop,
            TotalLength = trackRuntime.TotalLength
        };

        foreach (var pt in trackDef.Points)
        {
            trackMsg.Points.Add(new TrackPointMsg
            {
                Position = new Vector2Msg { X = pt.Position.X, Y = pt.Position.Y },
                Width = pt.Width
            });
        }

        return new ServerMessage
        {
            RaceAssigned = new RaceAssigned
            {
                RaceId = raceId.ToString(),
                TrackId = trackDef.Id,
                Rules = new Contracts.V1.RaceRules
                {
                    TickRate = (uint)rules.TickRate,
                    ActionTimeoutMs = (uint)rules.ActionTimeoutMs,
                    MaxConsecutiveTimeouts = (uint)rules.MaxConsecutiveTimeouts,
                    TotalLaps = (uint)rules.TotalLaps
                },
                Track = trackMsg
            }
        };
    }
}
