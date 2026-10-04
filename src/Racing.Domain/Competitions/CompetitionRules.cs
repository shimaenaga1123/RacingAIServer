namespace Racing.Domain.Competitions;

public sealed record CompetitionRules
{
    public int TickRate { get; init; } = 60;
    public int ActionTimeoutMs { get; init; } = 50;
    public int MaxConsecutiveTimeouts { get; init; } = 10;
    public int TotalLaps { get; init; } = 3;
    public int MaxParticipants { get; init; } = 8;
}
