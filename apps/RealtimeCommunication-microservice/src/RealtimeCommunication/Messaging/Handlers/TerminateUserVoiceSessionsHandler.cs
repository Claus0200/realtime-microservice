
using RealtimeCommunication.Services;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;

namespace RealtimeCommunication.Messaging.Handlers;

public sealed class TerminateUserVoiceSessionsHandler(
    IVoiceMediaAdmin media,
    IVoiceAccessStore access,
    IMessageClient messageClient,
    ILogger<TerminateUserVoiceSessionsHandler> logger)
    : IMessageHandler<TerminateUserVoiceSessions>
{
    public async Task HandleAsync(
        TerminateUserVoiceSessions message,
        CancellationToken cancellationToken)
    {
        if (message.SagaId == Guid.Empty ||
            message.UserId == Guid.Empty)
            throw new ArgumentException("Invalid saga command.");

        try
        {
            // Prevent the user from receiving new voice tokens.
            await access.BlockAsync(message.UserId);

            // Remove the user from every active LiveKit voice room.
            var disconnected = await media.DisconnectUserAsync(
                message.UserId, cancellationToken);

            logger.LogInformation(
                "Saga {SagaId}: Disconnected user {UserId} from {Count} rooms",
                message.SagaId,
                message.UserId,
                disconnected);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Saga {SagaId}: Failed to terminate voice sessions",
                message.SagaId);

            await messageClient.PublishAsync(
                new VoiceSessionTerminationFailed
                {
                    SagaId = message.SagaId,
                    UserId = message.UserId,
                    Reason = ex.Message
                },
                cancellationToken);

            return;
        }

        // Report success only after blocking and disconnection succeed.
        // Publishing failures must propagate for message retry handling.
        await messageClient.PublishAsync(
            new VoiceSessionsTerminated
            {
                SagaId = message.SagaId,
                UserId = message.UserId
            },
            cancellationToken);
    }
}
