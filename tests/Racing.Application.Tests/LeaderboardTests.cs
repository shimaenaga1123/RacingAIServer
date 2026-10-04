using System;
using System.Collections.Generic;
using Racing.Application.Leaderboard;
using Racing.Application.Persistence;
using Xunit;

namespace Racing.Application.Tests;

public class LeaderboardTests
{
    [Fact]
    public void LeaderboardCalculator_ShouldRankByPointsAndTieBreakers()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();

        var results = new List<PersistedRaceResult>
        {
            // Race 1
            new(Guid.NewGuid(), Guid.NewGuid(), p1, "DriverA", 1, 60.5f, 19.8f, 10, false, null, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), p2, "DriverB", 2, 61.2f, 20.1f, 8, false, null, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), p3, "DriverC", 3, 62.0f, 20.5f, 6, false, null, DateTimeOffset.UtcNow),

            // Race 2
            new(Guid.NewGuid(), Guid.NewGuid(), p2, "DriverB", 1, 59.8f, 19.5f, 10, false, null, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), p1, "DriverA", 2, 60.1f, 19.7f, 8, false, null, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), p3, "DriverC", 3, 61.5f, 20.3f, 6, false, null, DateTimeOffset.UtcNow),
        };

        var calculator = new LeaderboardCalculator();
        var leaderboard = calculator.Calculate(results);

        Assert.Equal(3, leaderboard.Count);

        // DriverA와 DriverB 둘 다 18점, 1승, 2포디움이지만
        // DriverB의 BestLap(19.5s)이 DriverA의 BestLap(19.7s)보다 빠르므로 DriverB가 1위
        Assert.Equal("DriverB", leaderboard[0].ParticipantName);
        Assert.Equal(1, leaderboard[0].Rank);
        Assert.Equal(18, leaderboard[0].TotalPoints);
        Assert.Equal(19.5f, leaderboard[0].BestLapTimeSeconds);

        Assert.Equal("DriverA", leaderboard[1].ParticipantName);
        Assert.Equal(2, leaderboard[1].Rank);
        Assert.Equal(18, leaderboard[1].TotalPoints);

        Assert.Equal("DriverC", leaderboard[2].ParticipantName);
        Assert.Equal(3, leaderboard[2].Rank);
        Assert.Equal(12, leaderboard[2].TotalPoints);
    }
}
