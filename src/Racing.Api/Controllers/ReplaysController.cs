using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Racing.Application.Persistence;

namespace Racing.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class ReplaysController(IReplayRepository replayRepository) : ControllerBase
{
    private readonly IReplayRepository _replayRepository = replayRepository;

    [HttpGet("{raceId:guid}")]
    public async Task<IActionResult> GetReplay(Guid raceId)
    {
        var replay = await _replayRepository.GetReplayAsync(raceId);
        if (replay == null)
        {
            return NotFound(new { error = $"Replay for race '{raceId}' not found." });
        }

        return Ok(replay);
    }
}
