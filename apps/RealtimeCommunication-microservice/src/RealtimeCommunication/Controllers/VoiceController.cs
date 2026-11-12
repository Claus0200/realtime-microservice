
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealtimeCommunication.Services;

namespace RealtimeCommunication.Controllers;

[ApiController]
[Authorize]
[Route("api/voice")]
public sealed class VoiceController(
    LiveKitTokenService tokens,
    IVoiceAccessStore access,
    IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("{channelId:guid}/join")]
    public async Task<IActionResult> Join(Guid channelId)
    {
        if (channelId == Guid.Empty)
            return BadRequest();

        if (!Guid.TryParse(
                User.FindFirst("sub")?.Value,
                out var userId))
            return Unauthorized();

        if (await access.IsBlockedAsync(userId))
            return Forbid();

        // Local integration only.
        // Before production, validate channel membership
        // and authoritative ban status in User Management.
        if (!environment.IsDevelopment())
            return StatusCode(
                503, "Channel authorization not configured");

        try
        {
            return Ok(new
            {
                roomName = $"voice-{channelId:N}",
                serverUrl = configuration["LiveKit:PublicUrl"],
                token = await tokens.CreateAsync(channelId, userId)
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
