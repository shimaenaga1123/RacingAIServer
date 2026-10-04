using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Racing.Api.Hubs;
using Racing.Simulation.Races;

namespace Racing.Api.Realtime;

public sealed class SpectatorBroadcaster
{
    private readonly IHubContext<RaceHub, IRaceSpectatorClient> _hubContext;

    public SpectatorBroadcaster(IHubContext<RaceHub, IRaceSpectatorClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async ValueTask BroadcastTickAsync(Race race, CancellationToken ct = default)
    {
        // 60Hz -> 15Hz (매 4틱마다 1회 브로드캐스트)
        if (race.CurrentTick % 4 != 0)
        {
            return;
        }

        var carDtos = new List<CarSnapshotDto>(race.Drivers.Count);
        foreach (var driver in race.Drivers)
        {
            carDtos.Add(new CarSnapshotDto(
                ParticipantId: driver.ParticipantId,
                Name: driver.Name,
                X: driver.Car.Position.X,
                Y: driver.Car.Position.Y,
                Rotation: driver.Car.Rotation,
                Speed: driver.Car.Speed,
                Steering: driver.Car.Steering,
                Lap: driver.Progress.CurrentLap,
                NextCheckpoint: driver.Progress.NextCheckpointIndex,
                DistanceAlongTrack: driver.TrackPosition.DistanceAlongTrack,
                LateralOffset: driver.TrackPosition.LateralOffset,
                IsOnTrack: driver.TrackPosition.IsOnTrack
            ));
        }

        var snapshot = new RaceSnapshotDto(
            RaceId: race.Id,
            Tick: race.CurrentTick,
            TimestampMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Cars: carDtos
        );

        string groupName = race.Id.ToString();
        await _hubContext.Clients.Group(groupName).ReceiveSnapshot(snapshot);
    }
}
