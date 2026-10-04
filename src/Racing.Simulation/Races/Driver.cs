using System;
using Racing.Domain.Cars;
using Racing.Domain.Geometry;
using Racing.Simulation.Laps;
using Racing.Simulation.Tracks;

namespace Racing.Simulation.Races;

public sealed class Driver
{
    public Guid ParticipantId { get; }
    public string Name { get; }
    public Car Car { get; }
    public DriverProgress Progress { get; }
    public TrackPosition TrackPosition { get; set; }
    public Vector2 PreviousPosition { get; set; }

    public Driver(Guid participantId, string name, Car car)
    {
        ParticipantId = participantId;
        Name = name;
        Car = car;
        Progress = new DriverProgress();
        TrackPosition = default;
        PreviousPosition = car.Position;
    }
}
