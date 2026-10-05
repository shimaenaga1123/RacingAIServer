using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Racing.Application.Auth;
using Racing.Domain.Auth;
using Racing.Domain.Competitions;

namespace Racing.Application.Competitions;

public sealed record JoinResult(
    Guid CompetitionId,
    Guid ParticipantId,
    string AgentName,
    string SessionToken);

public sealed class CompetitionManager(SessionTokenProvider? tokenProvider = null)
{
    private readonly ConcurrentDictionary<string, Competition> _competitionsByCode = new();
    private readonly ConcurrentDictionary<Guid, Competition> _competitionsById = new();
    private readonly ConcurrentDictionary<string, AgentSession> _sessionsByTokenHash = new();
    private readonly SessionTokenProvider _tokenProvider = tokenProvider ?? new SessionTokenProvider();

    public Competition CreateCompetition(
        string name,
        string trackId = "beginner-circuit",
        CompetitionRules? rules = null)
    {
        string code;
        do
        {
            code = JoinCodeGenerator.Generate(6);
        } while (_competitionsByCode.ContainsKey(code));

        var competition = new Competition(Guid.NewGuid(), name, code, trackId, rules);
        competition.Open(); // 바로 참가할 수 있도록 Open

        _competitionsByCode[code] = competition;
        _competitionsById[competition.Id] = competition;

        return competition;
    }

    public bool TryGetByCode(string code, out Competition? competition)
    {
        return _competitionsByCode.TryGetValue(code, out competition);
    }

    public bool TryGetById(Guid id, out Competition? competition)
    {
        return _competitionsById.TryGetValue(id, out competition);
    }

    public JoinResult Join(string code, string agentName)
    {
        if (!_competitionsByCode.TryGetValue(code, out var competition))
        {
            throw new KeyNotFoundException($"Competition with code '{code}' not found.");
        }

        var participant = competition.AddParticipant(agentName);
        var session = _tokenProvider.CreateSession(participant.Id, out string rawToken);

        _sessionsByTokenHash[session.TokenHash] = session;

        return new JoinResult(
            CompetitionId: competition.Id,
            ParticipantId: participant.Id,
            AgentName: participant.Name,
            SessionToken: rawToken
        );
    }

    public bool ValidateToken(string rawToken, out Guid participantId)
    {
        participantId = Guid.Empty;
        string hash = SessionTokenProvider.ComputeHash(rawToken);

        if (_sessionsByTokenHash.TryGetValue(hash, out var session) && session.IsValid)
        {
            participantId = session.ParticipantId;
            return true;
        }

        return false;
    }

    public ICollection<Competition> GetAll() => _competitionsById.Values;
}
