using System;
using Racing.Domain.Cars;
using Racing.Domain.Geometry;
using Racing.Simulation.Laps;
using Racing.Simulation.Tracks;

namespace Racing.Simulation.Races;

public sealed class Driver(Guid participantId, string name, Car car)
{
    public Guid ParticipantId { get; } = participantId;
    public string Name { get; } = name;
    public Car Car { get; } = car;
    public DriverProgress Progress { get; } = new DriverProgress();
    public TrackPosition TrackPosition { get; set; } = default;
    public Vector2 PreviousPosition { get; set; } = car.Position;
}
