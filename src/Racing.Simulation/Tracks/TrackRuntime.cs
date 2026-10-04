using System.Collections.Generic;
using Racing.Domain.Geometry;
using Racing.Domain.Tracks;

namespace Racing.Simulation.Tracks;

public sealed class TrackRuntime
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsLoop { get; init; }
    public required float TotalLength { get; init; }
    public required int DefaultLaps { get; init; }

    public required Vector2[] Centerline { get; init; }
    public required Vector2[] Tangents { get; init; }
    public required Vector2[] Normals { get; init; }
    public required float[] Widths { get; init; }
    public required float[] DistanceAlongTrack { get; init; }

    public required Vector2[] LeftBoundaries { get; init; }
    public required Vector2[] RightBoundaries { get; init; }

    public required IReadOnlyList<CheckpointDefinition> Checkpoints { get; init; }

    public int SampleCount => Centerline.Length;
}
