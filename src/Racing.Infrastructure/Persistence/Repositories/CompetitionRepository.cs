using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Racing.Application.Persistence;
using Racing.Domain.Competitions;
using Racing.Infrastructure.Persistence.Entities;

namespace Racing.Infrastructure.Persistence.Repositories;

public sealed class CompetitionRepository : ICompetitionRepository
{
    private readonly RacingDbContext _db;

    public CompetitionRepository(RacingDbContext db)
    {
        _db = db;
    }

    public async Task<Competition?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Competitions
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return entity == null ? null : MapToDomain(entity);
    }

    public async Task<Competition?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var entity = await _db.Competitions
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Code == code, ct);

        return entity == null ? null : MapToDomain(entity);
    }

    public async Task AddAsync(Competition competition, CancellationToken ct = default)
    {
        var entity = MapToEntity(competition);
        await _db.Competitions.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Competition competition, CancellationToken ct = default)
    {
        var entity = await _db.Competitions
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == competition.Id, ct);

        if (entity == null) return;

        entity.Status = competition.Status.ToString();

        // 새로 추가된 참가자 동기화
        var existingParticipantIds = entity.Participants.Select(p => p.Id).ToHashSet();
        foreach (var p in competition.Participants)
        {
            if (!existingParticipantIds.Contains(p.Id))
            {
                entity.Participants.Add(new ParticipantEntity
                {
                    Id = p.Id,
                    CompetitionId = competition.Id,
                    Name = p.Name,
                    RegisteredAt = p.RegisteredAt
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Competition>> ListAllAsync(CancellationToken ct = default)
    {
        var entities = await _db.Competitions
            .Include(c => c.Participants)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(MapToDomain).ToList();
    }

    private static Competition MapToDomain(CompetitionEntity entity)
    {
        var rules = new CompetitionRules
        {
            TickRate = entity.TickRate,
            ActionTimeoutMs = entity.ActionTimeoutMs,
            MaxConsecutiveTimeouts = entity.MaxConsecutiveTimeouts,
            TotalLaps = entity.TotalLaps,
            MaxParticipants = entity.MaxParticipants
        };

        var comp = new Competition(
            id: entity.Id,
            name: entity.Name,
            code: entity.Code,
            trackId: entity.TrackId,
            rules: rules
        );

        if (Enum.TryParse<CompetitionStatus>(entity.Status, out var status))
        {
            if (status >= CompetitionStatus.Open) comp.Open();
            foreach (var p in entity.Participants)
            {
                try { comp.AddParticipant(p.Name); } catch { }
            }
            if (status >= CompetitionStatus.Running) comp.Start();
            if (status == CompetitionStatus.Finished) comp.Finish();
        }

        return comp;
    }

    private static CompetitionEntity MapToEntity(Competition comp)
    {
        return new CompetitionEntity
        {
            Id = comp.Id,
            Name = comp.Name,
            Code = comp.Code,
            TrackId = comp.TrackId,
            Status = comp.Status.ToString(),
            CreatedAt = comp.CreatedAt,
            TickRate = comp.Rules.TickRate,
            ActionTimeoutMs = comp.Rules.ActionTimeoutMs,
            MaxConsecutiveTimeouts = comp.Rules.MaxConsecutiveTimeouts,
            TotalLaps = comp.Rules.TotalLaps,
            MaxParticipants = comp.Rules.MaxParticipants,
            Participants = comp.Participants.Select(p => new ParticipantEntity
            {
                Id = p.Id,
                CompetitionId = comp.Id,
                Name = p.Name,
                RegisteredAt = p.RegisteredAt
            }).ToList()
        };
    }
}
