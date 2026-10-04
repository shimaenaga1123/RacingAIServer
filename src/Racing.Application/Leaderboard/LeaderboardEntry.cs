using System;

namespace Racing.Application.Leaderboard;

public sealed record LeaderboardEntry(
    int Rank,
    Guid ParticipantId,
    string ParticipantName,
    int TotalPoints,
    int RacesParticipated,
    int Wins,
    int Podiums,
    float BestLapTimeSeconds,
    float TotalTimeSeconds);
