using System;
using System.Collections.Generic;
using Racing.Domain.Geometry;

namespace Racing.Simulation.Observations;

public sealed record PlayerObservation(
    Vector2 Position,
    Vector2 Velocity,
    float Speed,
    float Rotation,
    float AngularVelocity,
    float Steering,
    float Throttle,
    float Brake);

public sealed record TrackObservation(
    float DistanceAlongTrack,
    float LateralOffset,
    float HeadingError,
    float TrackWidth,
    IReadOnlyList<float> AheadCurvatures);

public sealed record LapObservation(
    int CurrentLap,
    int TotalLaps,
    float CurrentLapTime,
    float LastLapTime,
    float BestLapTime,
    int NextCheckpointIndex,
    int TotalCheckpoints);

public sealed record OpponentObservation(
    Guid ParticipantId,
    string Name,
    Vector2 Position,
    Vector2 Velocity,
    float Speed,
    float Rotation,
    float DistanceAlongTrack,
    float LateralOffset);

public sealed record Observation(
    Guid RaceId,
    ulong Tick,
    long TimestampMs,
    PlayerObservation Player,
    TrackObservation Track,
    LapObservation Lap,
    IReadOnlyList<OpponentObservation> Opponents);
