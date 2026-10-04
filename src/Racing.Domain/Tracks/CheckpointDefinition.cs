using Racing.Domain.Geometry;

namespace Racing.Domain.Tracks;

public sealed record CheckpointDefinition(
    int Index,
    Vector2 Center,
    Segment2D Line,
    float DistanceAlongTrack);
