namespace Racing.Simulation.Tracks;

public readonly record struct TrackPosition(
    float DistanceAlongTrack,
    float LateralOffset,
    float HeadingError,
    float TrackWidth,
    bool IsOnTrack);
