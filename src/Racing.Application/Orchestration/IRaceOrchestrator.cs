using System;
using System.Threading;
using System.Threading.Tasks;
using Racing.Domain.Competitions;
using Racing.Simulation.Races;

namespace Racing.Application.Orchestration;

public interface IRaceOrchestrator
{
    Task<Race> RunRaceAsync(
        Competition competition,
        Func<Race, CancellationToken, ValueTask>? onTickBroadcast = null,
        CancellationToken ct = default);

    Race? GetActiveRace(Guid competitionId);
}
