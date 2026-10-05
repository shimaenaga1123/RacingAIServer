using System;
using System.Collections.Generic;

namespace Racing.Infrastructure.Persistence.Entities;

public sealed class CompetitionEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string TrackId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public int TickRate { get; set; }
    public int ActionTimeoutMs { get; set; }
    public int MaxConsecutiveTimeouts { get; set; }
    public int TotalLaps { get; set; }
    public int MaxParticipants { get; set; }

    public List<ParticipantEntity> Participants { get; set; } = [];
}

public sealed class ParticipantEntity
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset RegisteredAt { get; set; }

    public CompetitionEntity? Competition { get; set; }
}

public sealed class AgentSessionEntity
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}

public sealed class RaceResultEntity
{
    public Guid Id { get; set; }
    public Guid RaceId { get; set; }
    public Guid CompetitionId { get; set; }
    public Guid ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public float TotalTimeSeconds { get; set; }
    public float FastestLapSeconds { get; set; }
    public int PointsAwarded { get; set; }
    public bool IsDnf { get; set; }
    public string? DnfReason { get; set; }
    public DateTimeOffset FinishedAt { get; set; }
}
