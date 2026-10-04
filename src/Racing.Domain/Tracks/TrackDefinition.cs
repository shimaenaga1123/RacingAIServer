using System.Collections.Generic;

namespace Racing.Domain.Tracks;

public sealed class TrackDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<TrackPoint> Points { get; init; }
    public bool IsLoop { get; init; } = true;
    public int DefaultLaps { get; init; } = 3;
}
