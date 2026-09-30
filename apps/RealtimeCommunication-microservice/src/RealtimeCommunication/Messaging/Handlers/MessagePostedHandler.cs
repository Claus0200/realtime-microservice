using Shared.Contracts.Events;

namespace RealtimeCommunication.Messaging.Handlers;

public sealed class MessagePostedHandler
    : IMessageHandler<MessagePostedEvent>
{
    private readonly IMessageClient _messageClient;
    private readonly ILogger<MessagePostedHandler> _logger;

    public MessagePostedHandler(
        IMessageClient messageClient,
        ILogger<MessagePostedHandler> logger)
    {
        _messageClient = messageClient;
        _logger = logger;
    }

    public async Task HandleAsync(
        MessagePostedEvent message,
        CancellationToken cancellationToken)
    {
        var content = message.Content
            .Trim()
            .ToLowerInvariant();

        var command = content switch
        {
            "mute" or "/mute" => "mute",
            "unmute" or "/unmute" => "unmute",
            "deafen" or "/deafen" => "deafen",
            "undeafen" or "/undeafen" => "undeafen",
            _ => null
        };

        _logger.LogInformation(
            "Processed MessagePostedEvent {MessageId}. Voice command: {Command}",
            message.MessageId,
            command ?? "none");

        await _messageClient.PublishAsync(
            new VoiceCommandCheckedEvent
            {
                MessageId = message.MessageId,
                ChannelId = message.ChannelId,
                AuthorId = message.AuthorId,
                VoiceCommandDetected = command is not null,
                Command = command,
                ProcessedAt = DateTime.UtcNow
            },
            cancellationToken);
    }
}