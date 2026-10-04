using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Racing.Api.Hubs;

public sealed class RaceHub : Hub<IRaceSpectatorClient>
{
    public async Task JoinRace(string raceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, raceId);
    }

    public async Task LeaveRace(string raceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, raceId);
    }
}
