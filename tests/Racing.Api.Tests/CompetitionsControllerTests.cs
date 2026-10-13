using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Racing.Api.Controllers;
using Racing.Api.Hubs;
using Racing.Api.Realtime;
using Racing.Application.Auth;
using Racing.Application.Competitions;
using Racing.Application.Orchestration;
using Racing.Application.Persistence;
using Racing.Domain.Competitions;
using Racing.Simulation.Tracks;
using Xunit;

namespace Racing.Api.Tests;

public class CompetitionsControllerTests
{
    private readonly CompetitionManager _manager;
    private readonly Mock<IRaceOrchestrator> _orchestratorMock;
    private readonly Mock<IRaceResultRepository> _resultRepoMock;
    private readonly Mock<IHubContext<RaceHub, IRaceSpectatorClient>> _hubContextMock;
    private readonly SpectatorBroadcaster _broadcaster;
    private readonly CompetitionsController _controller;

    public CompetitionsControllerTests()
    {
        _manager = new CompetitionManager(new SessionTokenProvider());
        _orchestratorMock = new Mock<IRaceOrchestrator>();
        _resultRepoMock = new Mock<IRaceResultRepository>();

        var clientsMock = new Mock<IHubClients<IRaceSpectatorClient>>();
        var clientProxyMock = new Mock<IRaceSpectatorClient>();
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);

        _hubContextMock = new Mock<IHubContext<RaceHub, IRaceSpectatorClient>>();
        _hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        _broadcaster = new SpectatorBroadcaster(_hubContextMock.Object);

        _controller = new CompetitionsController(
            _manager,
            _orchestratorMock.Object,
            _broadcaster,
            _resultRepoMock.Object
        );
    }

    [Fact]
    public void CreateCompetitionShouldReturnCreatedAtActionResult()
    {
        var request = new CompetitionsController.CreateCompetitionRequest("Championship #1", "beginner-circuit", 3, 8);
        var result = _controller.Create(request) as CreatedAtActionResult;

        Assert.NotNull(result);
        Assert.Equal(201, result.StatusCode);
    }

    [Fact]
    public void GetByCodeShouldReturnOkWhenExists()
    {
        var comp = _manager.CreateCompetition("Test Cup");
        var result = _controller.GetByCode(comp.Code) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public void JoinShouldReturnOkWithSessionToken()
    {
        var comp = _manager.CreateCompetition("Test Cup");
        var request = new CompetitionsController.JoinCompetitionRequest("AlphaRacer", "0.1.0");

        var result = _controller.Join(comp.Code, request) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public void StartShouldTriggerOrchestrator()
    {
        var comp = _manager.CreateCompetition("Test Cup");
        comp.AddParticipant("Racer1");

        var result = _controller.Start(comp.Code) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task GetLeaderboardShouldReturnOk()
    {
        var comp = _manager.CreateCompetition("Test Cup");
        comp.AddParticipant("Racer1");

        _resultRepoMock.Setup(r => r.GetResultsByCompetitionAsync(comp.Id, default))
            .ReturnsAsync(new List<PersistedRaceResult>());

        var result = await _controller.GetLeaderboard(comp.Code) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }
}
