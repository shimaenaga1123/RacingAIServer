using System.Collections.Generic;

namespace Racing.Simulation.Laps;

public sealed class DriverProgress
{
    public int CurrentLap { get; set; } = 1;
    public int NextCheckpointIndex { get; set; } = 1;
    public bool IsFinished { get; set; } = false;
    public bool IsDisqualified { get; set; } = false;
    public string? DnfReason { get; set; }

    public float CurrentLapTimeSeconds { get; set; } = 0f;
    public float LastLapTimeSeconds { get; set; } = 0f;
    public float BestLapTimeSeconds { get; set; } = float.MaxValue;
    public float TotalRaceTimeSeconds { get; set; } = 0f;

    public List<float> LapTimes { get; } = [];

    public float LastRecordedDistance { get; set; } = 0f;
    public int ConsecutiveWrongWayTicks { get; set; } = 0;
}
