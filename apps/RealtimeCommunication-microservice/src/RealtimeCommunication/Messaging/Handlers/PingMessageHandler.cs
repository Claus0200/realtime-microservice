using Messages;

namespace RealtimeCommunication.Messaging.Handlers;

public sealed class PingMessageHandler
    : IMessageHandler<PingMessage>
{
    private readonly ILogger<PingMessageHandler> _logger;

    public PingMessageHandler(
        ILogger<PingMessageHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(
        PingMessage message,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received PingMessage: {@Message}",
            message);

        return Task.CompletedTask;
    }
}
