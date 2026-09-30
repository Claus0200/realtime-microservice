using Microsoft.AspNetCore.Mvc;
using RealtimeCommunication.Services;
using RealtimeCommunication.Shared.Requests;

namespace RealtimeCommunication.Controllers;

[ApiController]
[Route("api/realtime-sessions")]
public sealed class RealtimeSessionsController : ControllerBase
{
    private readonly IRealtimeSessionService _service;

    public RealtimeSessionsController(IRealtimeSessionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var sessions = await _service.GetAllAsync(cancellationToken);

        return Ok(sessions);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var session = await _service.GetByIdAsync(
            id,
            cancellationToken);

        return session is null
            ? NotFound()
            : Ok(session);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateRealtimeSessionRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _service.CreateAsync(
            request,
            cancellationToken);

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
        try
        {
            var session = await _service.UpdateAsync(
                id,
                request,
                cancellationToken);

            return session is null
                ? NotFound()
                : Ok(session);
        }
        catch (InvalidOperationException exception)
        {
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
        var deleted = await _service.DeleteAsync(
            id,
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
    }
}
