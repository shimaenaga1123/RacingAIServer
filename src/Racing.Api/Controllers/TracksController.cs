using System;
using Microsoft.AspNetCore.Mvc;
using Racing.Simulation.Tracks;

namespace Racing.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class TracksController(ITrackLoader trackLoader) : ControllerBase
{
    private readonly ITrackLoader _trackLoader = trackLoader;

    [HttpGet("{trackId}")]
    public IActionResult GetTrack(string trackId)
    {
        try
        {
            var def = _trackLoader.LoadDefinition(trackId);
            var runtime = _trackLoader.LoadRuntime(trackId);

            return Ok(new
            {
                id = def.Id,
                name = def.Name,
                isLoop = def.IsLoop,
                totalLength = runtime.TotalLength,
                points = def.Points,
                centerline = runtime.Centerline,
                leftBoundaries = runtime.LeftBoundaries,
                rightBoundaries = runtime.RightBoundaries,
                checkpoints = runtime.Checkpoints
            });
        }
        catch (Exception ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
