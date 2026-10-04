using System;

namespace Racing.Domain.Auth;

public sealed class AgentSession
{
    public Guid Id { get; }
    public Guid ParticipantId { get; }
    public string TokenHash { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public bool IsRevoked { get; private set; }

    public AgentSession(Guid participantId, string tokenHash, TimeSpan duration)
    {
        Id = Guid.NewGuid();
        ParticipantId = participantId;
        TokenHash = tokenHash;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = CreatedAt.Add(duration);
        IsRevoked = false;
    }

    public bool IsValid => !IsRevoked && DateTimeOffset.UtcNow < ExpiresAt;

    public void Revoke()
    {
        IsRevoked = true;
    }
}
