using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Racing.Api.Hubs;

public sealed record CarSnapshotDto(
    Guid ParticipantId,
    string Name,
    float X,
    float Y,
    float Rotation,
    float Speed,
    float Steering,
    int Lap,
    int NextCheckpoint,
    float DistanceAlongTrack,
    float LateralOffset,
    bool IsOnTrack);

public sealed record RaceSnapshotDto(
    Guid RaceId,
    ulong Tick,
    long TimestampMs,
    IReadOnlyList<CarSnapshotDto> Cars);

public interface IRaceSpectatorClient
{
    Task ReceiveSnapshot(RaceSnapshotDto snapshot);
    Task RaceStarted(Guid raceId);
    Task RaceFinished(Guid raceId);
}
