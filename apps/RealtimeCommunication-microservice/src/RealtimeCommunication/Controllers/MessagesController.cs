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
        await messageClient.PublishAsync(
            new BizcordPingMessage("Hello from Bizcord"),
            cancellationToken);

        return Accepted();
    }
}