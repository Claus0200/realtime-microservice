using Microsoft.Extensions.DependencyInjection;

namespace RealtimeCommunication.Messaging;

public sealed class MessageHandlerBackgroundService<TMessage>
    : BackgroundService
{
    private readonly IMessageClient _messageClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MessageHandlerBackgroundService<TMessage>> _logger;

    public MessageHandlerBackgroundService(
        IMessageClient messageClient,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<MessageHandlerBackgroundService<TMessage>> logger)
    {
        _messageClient = messageClient;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var prefix =
            _configuration["Messaging:SubscriptionPrefix"]
            ?? "realtime-communication";

        var subscriptionId =
            $"{prefix}-{typeof(TMessage).Name}".ToLowerInvariant();

        _logger.LogInformation(
            "Registering message subscription {SubscriptionId} for {MessageType}",
            subscriptionId,
            typeof(TMessage).FullName);

        await _messageClient.SubscribeAsync<TMessage>(
            subscriptionId,
            (message, cancellationToken) =>
                DispatchAsync(
                    message,
                    cancellationToken));

        try
        {
            await Task.Delay(
                Timeout.Infinite,
                stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task DispatchAsync(
        TMessage message,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var handlers = scope.ServiceProvider
            .GetServices<IMessageHandler<TMessage>>()
            .ToArray();

        if (handlers.Length == 0)
        {
            _logger.LogWarning(
                "A {MessageType} message was received but no handlers were registered.",
                typeof(TMessage).FullName);

            return;
        }

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(
                message,
                cancellationToken);
        }
    }
}