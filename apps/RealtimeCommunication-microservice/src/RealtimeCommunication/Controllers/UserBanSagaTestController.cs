
using Microsoft.AspNetCore.Mvc;
using RealtimeCommunication.Messaging;
using RealtimeCommunication.Repositories;
using Shared.Contracts.Commands;

namespace RealtimeCommunication.Controllers;

[ApiController]
[Route("internal/user-ban-saga-test")]
public sealed class UserBanSagaTestController(
    IMessageClient messages,
    IUserBanSagaRepository repository,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    private bool Enabled =>
        environment.IsDevelopment() &&
        configuration.GetValue<bool>("Voice:AllowSagaTestCommands");

    [HttpPost("start/{userId:guid}")]
    public async Task<IActionResult> Start(
        Guid userId,
        CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        if (userId == Guid.Empty) return BadRequest();

        var sagaId = Guid.NewGuid();

        await messages.PublishAsync(new InitiateUserBan
        {
            SagaId = sagaId,
            UserId = userId
        }, ct);

        return Accepted(new { sagaId, userId });
    }

    [HttpGet("status/{sagaId:guid}")]
    public async Task<IActionResult> Status(Guid sagaId)
    {
        if (!Enabled) return NotFound();
        if (sagaId == Guid.Empty) return BadRequest();

        var state = await repository.GetById(sagaId);

        return state is null
            ? NotFound()
            : Ok(state);
    }
}
