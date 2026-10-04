using System;
using Racing.Application.Competitions;
using Racing.Domain.Competitions;
using Xunit;

namespace Racing.Application.Tests;

public class CompetitionFlowTests
{
    [Fact]
    public void JoinCodeGenerator_ShouldGenerate6Digits()
    {
        string code = JoinCodeGenerator.Generate(6);
        Assert.Equal(6, code.Length);
        Assert.All(code, c => Assert.True(char.IsDigit(c)));
    }

    [Fact]
    public void CompetitionManager_CreateAndJoin_ShouldWorkSuccessfully()
    {
        var manager = new CompetitionManager();
        var comp = manager.CreateCompetition("Test Cup", "beginner-circuit");

        Assert.Equal(6, comp.Code.Length);
        Assert.Equal(CompetitionStatus.Open, comp.Status);

        // 참가 신청
        var joinResult = manager.Join(comp.Code, "AgentAlpha");
        Assert.Equal(comp.Id, joinResult.CompetitionId);
        Assert.NotEqual(Guid.Empty, joinResult.ParticipantId);
        Assert.StartsWith("rse_", joinResult.SessionToken);

        // 토큰 검증
        bool isValid = manager.ValidateToken(joinResult.SessionToken, out var participantId);
        Assert.True(isValid);
        Assert.Equal(joinResult.ParticipantId, participantId);
    }

    [Fact]
    public void CompetitionManager_DuplicateName_ShouldThrow()
    {
        var manager = new CompetitionManager();
        var comp = manager.CreateCompetition("Test Cup");

        manager.Join(comp.Code, "DuplicateBot");
        Assert.Throws<InvalidOperationException>(() => manager.Join(comp.Code, "DuplicateBot"));
    }
}
