using Microsoft.AspNetCore.Mvc;
using RealtimeCommunication.Services;

namespace RealtimeCommunication.Controllers;

[ApiController]
[Route("api/voice")]
public sealed class VoiceDevController(
    LiveKitTokenService tokens,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("{channelId:guid}/dev-token")]
    public IActionResult GetDevelopmentToken(
        Guid channelId,
        [FromQuery] Guid userId)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        if (channelId == Guid.Empty || userId == Guid.Empty)
            return BadRequest();

        return Ok(new
        {
            roomName = $"voice-{channelId:N}",
            serverUrl = "ws://localhost:7880",
            token = tokens.Create(channelId, userId)
        });
    }
}