
using Microsoft.AspNetCore.Mvc;
using RealtimeCommunication.Messaging;
using BizcordPingMessage = Messages.PingMessage;

namespace RealtimeCommunication.Controllers;

[ApiController]
[Route("api/messages")]
public class MessagesController(
    IMessageClient messageClient) : ControllerBase
{
    [HttpPost("ping")]
    public async Task<IActionResult> Ping(
        CancellationToken cancellationToken)
    {
        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await messageClient.PublishAsync(
                new BizcordPingMessage("Hello from Bizcord"),
                timeoutCts.Token);

            return Accepted();
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { error = "Message publishing timed out." });
        }
        catch (Exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { error = "Unable to publish message." });
        }
    }
}
