
using EasyNetQ;
using EasyNetQ.Persistent;

namespace RealtimeCommunication.Messaging;

public sealed class RabbitMqConnectionMonitor(
    IBus bus,
    ILogger<RabbitMqConnectionMonitor> logger)
    : BackgroundService
{
    private static readonly TimeSpan CheckInterval =
        TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        bool? wasConnected = null;

        using var timer = new PeriodicTimer(CheckInterval);

        try
        {
            do
            {
                var status = bus.Advanced.GetConnectionStatus(
                    PersistentConnectionType.Consumer);

                bool isConnected =
                    status.State == PersistentConnectionState.Connected;

                if (isConnected)
                {
                    // Only log when first connected or reconnected.
                    if (wasConnected != true)
                    {
                        logger.LogInformation(
                            "RabbitMQ connected successfully.");
                    }
                }
                else
                {
                    if (wasConnected == true)
                    {
                        logger.LogWarning(
                            "RabbitMQ connection lost. Waiting for reconnection...");
                    }
                    else
                    {
                        logger.LogWarning(
                            "RabbitMQ not connected (state: {State}). Checking again in 30 seconds...",
                            status.State);
                    }
                }

                wasConnected = isConnected;

            } while (
                await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
    }
}
