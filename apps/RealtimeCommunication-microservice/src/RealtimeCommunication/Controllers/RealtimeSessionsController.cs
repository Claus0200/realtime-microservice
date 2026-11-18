using Microsoft.AspNetCore.Mvc;
using RealtimeCommunication.Services;
using RealtimeCommunication.Shared.Requests;

namespace RealtimeCommunication.Controllers;

[ApiController]
[Route("api/realtime-sessions")]
public sealed class RealtimeSessionsController : ControllerBase
{
    private readonly IRealtimeSessionService _service;
    private ILogger<RealtimeSessionsController> _logger;

    public RealtimeSessionsController(IRealtimeSessionService service, ILogger<RealtimeSessionsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all realtime sessions.");

        var sessions = await _service.GetAllAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully retrieved {SessionCount} realtime sessions.",
            sessions.Count());

        return Ok(sessions);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving realtime session {SessionId}.",
            id);

        var session = await _service.GetByIdAsync(
            id,
            cancellationToken);

        if (session is null)
        {
            _logger.LogWarning(
                "Realtime session {SessionId} was not found.",
                id);

            return NotFound();
        }

        _logger.LogInformation(
            "Successfully retrieved realtime session {SessionId}.",
            id);

        return Ok(session);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateRealtimeSessionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating a new realtime session.");

        var session = await _service.CreateAsync(
            request,
            cancellationToken);

        _logger.LogInformation(
            "Successfully created realtime session {SessionId}.",
            session.Id);

        return CreatedAtAction(
            nameof(GetById),
            new { id = session.Id },
            session);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRealtimeSessionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating realtime session {SessionId}.",
            id);

        try
        {
            var session = await _service.UpdateAsync(
                id,
                request,
                cancellationToken);

            if (session is null)
            {
                _logger.LogWarning(
                    "Cannot update realtime session {SessionId}: session was not found.",
                    id);

                return NotFound();
            }

            _logger.LogInformation(
                "Successfully updated realtime session {SessionId}.",
                id);

            return Ok(session);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(
                exception,
                "Conflict while updating realtime session {SessionId}.",
                id);

            return Conflict(new
            {
                error = exception.Message
            });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Deleting realtime session {SessionId}.",
            id);

        var deleted = await _service.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
        {
            _logger.LogWarning(
                "Cannot delete realtime session {SessionId}: session was not found.",
                id);

            return NotFound();
        }

        _logger.LogInformation(
            "Successfully deleted realtime session {SessionId}.",
            id);

        return NoContent();
    }
}
