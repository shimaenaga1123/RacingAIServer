using System;
using System.Threading;
using System.Threading.Tasks;
using Racing.Simulation.Replay;

namespace Racing.Application.Persistence;

public interface IReplayRepository
{
    Task SaveReplayAsync(ReplayData replay, CancellationToken ct = default);
    Task<ReplayData?> GetReplayAsync(Guid raceId, CancellationToken ct = default);
}
