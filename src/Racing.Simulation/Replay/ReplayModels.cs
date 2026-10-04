using System;
using System.Collections.Generic;
using Racing.Domain.Cars;
using Racing.Domain.Geometry;

namespace Racing.Simulation.Replay;

public sealed record DriverInitialState(
    Guid ParticipantId,
    string Name,
    Vector2 Position,
    float Rotation);

public sealed record ReplayHeader(
    Guid RaceId,
    string TrackId,
    int TotalLaps,
    int RandomSeed,
    IReadOnlyList<DriverInitialState> InitialDrivers);

public sealed record ReplayFrame(
    ulong Tick,
    IReadOnlyDictionary<Guid, DriverAction> Actions);

public sealed class ReplayData
{
    public required ReplayHeader Header { get; init; }
    public required List<ReplayFrame> Frames { get; init; }
}
