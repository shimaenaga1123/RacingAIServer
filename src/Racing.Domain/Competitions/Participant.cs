using System;

namespace Racing.Domain.Competitions;

public sealed class Participant(Guid id, string name)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public DateTimeOffset RegisteredAt { get; } = DateTimeOffset.UtcNow;
}
