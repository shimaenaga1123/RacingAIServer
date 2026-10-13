using System;

namespace Racing.Domain.Competitions;

public sealed class Participant(Guid id, string name, DateTimeOffset? registeredAt = null)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public DateTimeOffset RegisteredAt { get; } = registeredAt ?? DateTimeOffset.UtcNow;
}
