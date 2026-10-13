using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Racing.Api.Controllers;
using Racing.Application.Persistence;
using Racing.Simulation.Replay;
using Racing.Simulation.Tracks;
using Xunit;

namespace Racing.Api.Tests;

public class AdditionalControllersTests
{
    [Fact]
    public void TracksControllerGetTrackShouldReturnOk()
    {
        var trackLoader = new TrackLoader();
        var controller = new TracksController(trackLoader);

        var result = controller.GetTrack("beginner-circuit") as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task ReplaysControllerGetReplayShouldReturnReplayWhenExists()
    {
        var mockRepo = new Mock<IReplayRepository>();
        var raceId = Guid.NewGuid();
        var replayData = new ReplayData
        {
            Header = new ReplayHeader(raceId, "beginner-circuit", 3, 42, []),
            Frames = []
        };

        mockRepo.Setup(r => r.GetReplayAsync(raceId, default)).ReturnsAsync(replayData);

        var controller = new ReplaysController(mockRepo.Object);
        var result = await controller.GetReplay(raceId) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task ReplaysControllerGetReplayShouldReturnNotFoundWhenMissing()
    {
        var mockRepo = new Mock<IReplayRepository>();
        var raceId = Guid.NewGuid();
        mockRepo.Setup(r => r.GetReplayAsync(raceId, default)).ReturnsAsync((ReplayData?)null);

        var controller = new ReplaysController(mockRepo.Object);
        var result = await controller.GetReplay(raceId) as NotFoundObjectResult;

        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }
}
