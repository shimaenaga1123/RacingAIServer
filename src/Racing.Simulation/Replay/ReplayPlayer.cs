using System;
using System.Collections.Generic;
using System.Linq;
using Racing.Domain.Cars;
using Racing.Simulation.Races;
using Racing.Simulation.Tracks;

namespace Racing.Simulation.Replay;

public sealed class ReplayPlayer
{
    private readonly ReplayData _replayData;
    private readonly TrackRuntime _track;
    private readonly RaceSimulator _simulator;

    public Race Race { get; private set; }
    public int CurrentFrameIndex { get; private set; } = 0;
    public int TotalFrames => _replayData.Frames.Count;
    public bool IsFinished => CurrentFrameIndex >= TotalFrames;

    public ReplayPlayer(ReplayData replayData, TrackRuntime track)
    {
        _replayData = replayData;
        _track = track;
        _simulator = new RaceSimulator(track, totalLaps: replayData.Header.TotalLaps);
        Race = InitializeRace();
    }

    public void Reset()
    {
        CurrentFrameIndex = 0;
        Race = InitializeRace();
    }

    public bool StepNext()
    {
        if (IsFinished) return false;

        var frame = _replayData.Frames[CurrentFrameIndex];
        _simulator.Step(Race, frame.Actions);
        CurrentFrameIndex++;
        return true;
    }

    public void StepAll()
    {
        while (StepNext()) { }
    }

    private Race InitializeRace()
    {
        var drivers = _replayData.Header.InitialDrivers.Select(d =>
        {
            var car = new Car(d.Position, d.Rotation);
            return new Driver(d.ParticipantId, d.Name, car);
        }).ToList();

        var race = new Race(
            _replayData.Header.RaceId,
            _track,
            drivers,
            _replayData.Header.TotalLaps
        );
        race.Start();
        return race;
    }
}
