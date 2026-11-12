
using Microsoft.AspNetCore.Mvc;
using RealtimeCommunication.Messaging;
using Shared.Contracts.Commands;

namespace RealtimeCommunication.Controllers;

[ApiController]
[Route("internal/voice-saga-test")]
public sealed class VoiceSagaTestController(
    IMessageClient bus,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    private bool Enabled =>
        environment.IsDevelopment() &&
        configuration.GetValue<bool>("Voice:AllowSagaTestCommands");

    [HttpPost("terminate/{userId:guid}")]
    public async Task<IActionResult> Terminate(
        Guid userId,
        CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        if (userId == Guid.Empty) return BadRequest();

        var sagaId = Guid.NewGuid();

        await bus.PublishAsync(
            new TerminateUserVoiceSessions
            {
                SagaId = sagaId,
                UserId = userId
            }, ct);

        return Accepted(new { sagaId, userId });
    }

    [HttpPost("restore/{userId:guid}")]
    public async Task<IActionResult> Restore(
        Guid userId,
        CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        if (userId == Guid.Empty) return BadRequest();

        var sagaId = Guid.NewGuid();

        await bus.PublishAsync(
            new RestoreUserVoiceAccess
            {
                SagaId = sagaId,
                UserId = userId
            }, ct);

        return Accepted(new { sagaId, userId });
    }
}
