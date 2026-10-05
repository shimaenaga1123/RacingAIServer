using System;
using System.Collections.Generic;
using Racing.Simulation.Tracks;

namespace Racing.Simulation.Races;

public sealed class Race(Guid id, TrackRuntime track, IReadOnlyList<Driver> drivers, int totalLaps = 3)
{
    public Guid Id { get; } = id;
    public TrackRuntime Track { get; } = track;
    public int TotalLaps { get; } = totalLaps;
    public IReadOnlyList<Driver> Drivers { get; } = drivers;

    public RaceStatus Status { get; private set; } = RaceStatus.Ready;
    public ulong CurrentTick { get; private set; } = 0;

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
