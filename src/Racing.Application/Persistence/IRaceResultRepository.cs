using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Racing.Application.Persistence;

public sealed record PersistedRaceResult(
    Guid RaceId,
    Guid CompetitionId,
    Guid ParticipantId,
    string ParticipantName,
    int Rank,
    float TotalTimeSeconds,
    float FastestLapSeconds,
    int PointsAwarded,
    bool IsDnf,
    string? DnfReason,
    DateTimeOffset FinishedAt);

public interface IRaceResultRepository
{
    Task SaveResultsAsync(IEnumerable<PersistedRaceResult> results, CancellationToken ct = default);
    Task<IReadOnlyList<PersistedRaceResult>> GetResultsByCompetitionAsync(Guid competitionId, CancellationToken ct = default);
    Task<IReadOnlyList<PersistedRaceResult>> GetResultsByRaceAsync(Guid raceId, CancellationToken ct = default);
}
