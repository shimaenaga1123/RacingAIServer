using System;

namespace Racing.Domain.Competitions;

public sealed class Participant
{
    public Guid Id { get; }
    public string Name { get; }
    public DateTimeOffset RegisteredAt { get; }

    public Participant(Guid id, string name)
    {
        Id = id;
        Name = name;
        RegisteredAt = DateTimeOffset.UtcNow;
    }
}
