using System;
using System.Collections.Generic;
using Racing.Simulation.Tracks;

namespace Racing.Simulation.Races;

public sealed class Race
{
    public Guid Id { get; }
    public TrackRuntime Track { get; }
    public int TotalLaps { get; }
    public IReadOnlyList<Driver> Drivers { get; }

    public RaceStatus Status { get; private set; } = RaceStatus.Ready;
    public ulong CurrentTick { get; private set; } = 0;

    public Race(Guid id, TrackRuntime track, IReadOnlyList<Driver> drivers, int totalLaps = 3)
    {
        Id = id;
        Track = track;
        Drivers = drivers;
        TotalLaps = totalLaps;
    }

    public void Start()
    {
        if (Status != RaceStatus.Ready)
        {
            throw new InvalidOperationException("Race can only be started from Ready status.");
        }
        Status = RaceStatus.Running;
    }

    public void Finish()
    {
        Status = RaceStatus.Finished;
    }

    public void AdvanceTick()
    {
        CurrentTick++;
    }
}
