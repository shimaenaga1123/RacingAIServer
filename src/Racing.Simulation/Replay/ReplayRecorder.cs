using System;
using System.Collections.Generic;
using System.Linq;
using Racing.Domain.Cars;
using Racing.Simulation.Races;

namespace Racing.Simulation.Replay;

public sealed class ReplayRecorder
{
    private readonly ReplayHeader _header;
    private readonly List<ReplayFrame> _frames = new();

    public ReplayRecorder(Race race, int seed = 42)
    {
        var initialDrivers = race.Drivers.Select(d => new DriverInitialState(
            ParticipantId: d.ParticipantId,
            Name: d.Name,
            Position: d.Car.Position,
            Rotation: d.Car.Rotation
        )).ToList();

        _header = new ReplayHeader(
            RaceId: race.Id,
            TrackId: race.Track.Id,
            TotalLaps: race.TotalLaps,
            RandomSeed: seed,
            InitialDrivers: initialDrivers
        );
    }

    public void RecordTick(ulong tick, IReadOnlyDictionary<Guid, DriverAction> actions)
    {
        // 불변 복사본 저장
        var frameActions = actions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        _frames.Add(new ReplayFrame(tick, frameActions));
    }

    public ReplayData Export()
    {
        return new ReplayData
        {
            Header = _header,
            Frames = new List<ReplayFrame>(_frames)
        };
    }
}
