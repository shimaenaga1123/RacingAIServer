using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Racing.Domain.Competitions;

namespace Racing.Application.Persistence;

public interface ICompetitionRepository
{
    Task<Competition?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Competition?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task AddAsync(Competition competition, CancellationToken ct = default);
    Task UpdateAsync(Competition competition, CancellationToken ct = default);
    Task<IReadOnlyList<Competition>> ListAllAsync(CancellationToken ct = default);
}
