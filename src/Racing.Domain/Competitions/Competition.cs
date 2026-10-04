using System;
using System.Collections.Generic;

namespace Racing.Domain.Competitions;

public sealed class Competition
{
    private readonly List<Participant> _participants = new();

    public Guid Id { get; }
    public string Name { get; }
    public string Code { get; }
    public string TrackId { get; }
    public CompetitionRules Rules { get; }
    public CompetitionStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyList<Participant> Participants => _participants;

    public Competition(
        Guid id,
        string name,
        string code,
        string trackId,
        CompetitionRules? rules = null)
    {
        Id = id;
        Name = name;
        Code = code;
        TrackId = trackId;
        Rules = rules ?? new CompetitionRules();
        Status = CompetitionStatus.Created;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Open()
    {
        if (Status != CompetitionStatus.Created)
        {
            throw new InvalidOperationException($"Cannot open competition in status {Status}");
        }
        Status = CompetitionStatus.Open;
    }

    public Participant AddParticipant(string name)
    {
        if (Status != CompetitionStatus.Open)
        {
            throw new InvalidOperationException($"Cannot join competition in status {Status}. Must be Open.");
        }

        if (_participants.Count >= Rules.MaxParticipants)
        {
            throw new InvalidOperationException("Competition is full.");
        }

        if (_participants.Exists(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Participant name '{name}' is already taken.");
        }

        var participant = new Participant(Guid.NewGuid(), name);
        _participants.Add(participant);
        return participant;
    }

    public void Start()
    {
        if (Status != CompetitionStatus.Open)
        {
            throw new InvalidOperationException("Competition must be Open to start.");
        }

        if (_participants.Count == 0)
        {
            throw new InvalidOperationException("Cannot start competition with 0 participants.");
        }

        Status = CompetitionStatus.Running;
    }

    public void Finish()
    {
        Status = CompetitionStatus.Finished;
    }
}
