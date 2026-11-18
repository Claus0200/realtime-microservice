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
    IConfiguration configuration,
    ILogger<UserBanSagaTestController> logger) : ControllerBase
{
    private bool Enabled =>
        environment.IsDevelopment() &&
        configuration.GetValue<bool>("Voice:AllowSagaTestCommands");

    [HttpPost("start/{userId:guid}")]
    public async Task<IActionResult> Start(
        Guid userId,
        CancellationToken ct)
    {
        if (!Enabled)
        {
            logger.LogWarning(
                "User ban saga test endpoint accessed while disabled.");

            return NotFound();
        }

        if (userId == Guid.Empty)
        {
            logger.LogWarning(
                "User ban saga initiation rejected: empty user ID.");

            return BadRequest();
        }

        var sagaId = Guid.NewGuid();

        logger.LogInformation(
            "Starting user ban saga {SagaId} for user {UserId}.",
            sagaId,
            userId);

        await messages.PublishAsync(new InitiateUserBan
        {
            SagaId = sagaId,
            UserId = userId
        }, ct);

        logger.LogInformation(
            "Published InitiateUserBan command for saga {SagaId} and user {UserId}.",
            sagaId,
            userId);

        return Accepted(new { sagaId, userId });
    }

    [HttpGet("status/{sagaId:guid}")]
    public async Task<IActionResult> Status(Guid sagaId)
    {
        if (!Enabled)
        {
            logger.LogWarning(
                "User ban saga status endpoint accessed while disabled.");

            return NotFound();
        }

        if (sagaId == Guid.Empty)
        {
            logger.LogWarning(
                "User ban saga status lookup rejected: empty saga ID.");

            return BadRequest();
        }

        logger.LogInformation(
            "Looking up user ban saga {SagaId}.",
            sagaId);

        var state = await repository.GetById(sagaId);

        if (state is null)
        {
            logger.LogWarning(
                "User ban saga {SagaId} was not found.",
                sagaId);

            return NotFound();
        }

        logger.LogInformation(
            "Successfully retrieved user ban saga {SagaId}.",
            sagaId);

        return Ok(state);
    }
}
