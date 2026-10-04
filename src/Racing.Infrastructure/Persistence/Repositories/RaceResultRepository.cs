using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Racing.Application.Persistence;
using Racing.Infrastructure.Persistence.Entities;

namespace Racing.Infrastructure.Persistence.Repositories;

public sealed class RaceResultRepository : IRaceResultRepository
{
    private readonly RacingDbContext _db;

    public RaceResultRepository(RacingDbContext db)
    {
        _db = db;
    }

    public async Task SaveResultsAsync(IEnumerable<PersistedRaceResult> results, CancellationToken ct = default)
    {
        var entities = results.Select(r => new RaceResultEntity
        {
            Id = Guid.NewGuid(),
            RaceId = r.RaceId,
            CompetitionId = r.CompetitionId,
            ParticipantId = r.ParticipantId,
            ParticipantName = r.ParticipantName,
            Rank = r.Rank,
            TotalTimeSeconds = r.TotalTimeSeconds,
            FastestLapSeconds = r.FastestLapSeconds,
            PointsAwarded = r.PointsAwarded,
            IsDnf = r.IsDnf,
            DnfReason = r.DnfReason,
            FinishedAt = r.FinishedAt
        });

        await _db.RaceResults.AddRangeAsync(entities, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PersistedRaceResult>> GetResultsByCompetitionAsync(Guid competitionId, CancellationToken ct = default)
    {
        var entities = await _db.RaceResults
            .Where(r => r.CompetitionId == competitionId)
            .OrderBy(r => r.Rank)
            .ToListAsync(ct);

        return entities.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<PersistedRaceResult>> GetResultsByRaceAsync(Guid raceId, CancellationToken ct = default)
    {
        var entities = await _db.RaceResults
            .Where(r => r.RaceId == raceId)
            .OrderBy(r => r.Rank)
            .ToListAsync(ct);

        return entities.Select(Map).ToList();
    }

    private static PersistedRaceResult Map(RaceResultEntity e) => new(
        RaceId: e.RaceId,
        CompetitionId: e.CompetitionId,
        ParticipantId: e.ParticipantId,
        ParticipantName: e.ParticipantName,
        Rank: e.Rank,
        TotalTimeSeconds: e.TotalTimeSeconds,
        FastestLapSeconds: e.FastestLapSeconds,
        PointsAwarded: e.PointsAwarded,
        IsDnf: e.IsDnf,
        DnfReason: e.DnfReason,
        FinishedAt: e.FinishedAt
    );
}
