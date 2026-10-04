using System;
using System.Collections.Generic;
using System.Linq;
using Racing.Application.Persistence;

namespace Racing.Application.Leaderboard;

public sealed class LeaderboardCalculator
{
    public IReadOnlyList<LeaderboardEntry> Calculate(IEnumerable<PersistedRaceResult> results)
    {
        var grouped = results.GroupBy(r => r.ParticipantId);

        var unsorted = grouped.Select(group =>
        {
            var first = group.First();
            int totalPoints = group.Sum(r => r.PointsAwarded);
            int participated = group.Count();
            int wins = group.Count(r => r.Rank == 1 && !r.IsDnf);
            int podiums = group.Count(r => r.Rank >= 1 && r.Rank <= 3 && !r.IsDnf);
            float bestLap = group.Where(r => r.FastestLapSeconds > 0)
                                 .Select(r => r.FastestLapSeconds)
                                 .DefaultIfEmpty(0f)
                                 .Min();
            float totalTime = group.Sum(r => r.TotalTimeSeconds);

            return new
            {
                ParticipantId = group.Key,
                ParticipantName = first.ParticipantName,
                TotalPoints = totalPoints,
                Participated = participated,
                Wins = wins,
                Podiums = podiums,
                BestLap = bestLap,
                TotalTime = totalTime
            };
        });

        // 정렬 순서: 점수 내림차순 -> 승수 내림차순 -> 포디움 내림차순 -> 베스트랩 오름차순
        var sorted = unsorted
            .OrderByDescending(x => x.TotalPoints)
            .ThenByDescending(x => x.Wins)
            .ThenByDescending(x => x.Podiums)
            .ThenBy(x => x.BestLap == 0f ? float.MaxValue : x.BestLap)
            .ToList();

        var leaderboard = new List<LeaderboardEntry>(sorted.Count);
        for (int i = 0; i < sorted.Count; i++)
        {
            var item = sorted[i];
            leaderboard.Add(new LeaderboardEntry(
                Rank: i + 1,
                ParticipantId: item.ParticipantId,
                ParticipantName: item.ParticipantName,
                TotalPoints: item.TotalPoints,
                RacesParticipated: item.Participated,
                Wins: item.Wins,
                Podiums: item.Podiums,
                BestLapTimeSeconds: item.BestLap,
                TotalTimeSeconds: item.TotalTime
            ));
        }

        return leaderboard;
    }
}
