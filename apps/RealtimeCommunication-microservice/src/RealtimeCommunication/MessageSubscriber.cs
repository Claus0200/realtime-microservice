using RealtimeCommunication.Messaging;
using BizcordPingMessage = Messages.PingMessage;

namespace RealtimeCommunication;

public sealed class MessageSubscriber(
    IMessageClient messageClient,
    ILogger<MessageSubscriber> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await messageClient.SubscribeAsync<BizcordPingMessage>(
            "realtime-communication-ping",
            (message, cancellationToken) =>
            {
                logger.LogInformation(
                    "Received ping: {Text}",
                    message.Text);

                return Task.CompletedTask;
            },
            stoppingToken);

        await Task.Delay(
            Timeout.Infinite,
            stoppingToken);
    }
}