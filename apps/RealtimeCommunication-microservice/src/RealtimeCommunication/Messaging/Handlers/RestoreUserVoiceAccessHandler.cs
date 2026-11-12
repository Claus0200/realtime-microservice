
using RealtimeCommunication.Services;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;

namespace RealtimeCommunication.Messaging.Handlers;

public sealed class RestoreUserVoiceAccessHandler(
    IVoiceAccessStore access,
    IMessageClient messageClient,
    ILogger<RestoreUserVoiceAccessHandler> logger)
    : IMessageHandler<RestoreUserVoiceAccess>
{
    public async Task HandleAsync(
        RestoreUserVoiceAccess message,
        CancellationToken cancellationToken)
    {
        if (message.SagaId == Guid.Empty ||
            message.UserId == Guid.Empty)
            throw new ArgumentException("Invalid saga command.");

        try
        {
            // Restore permission to obtain voice tokens.
            // Previously terminated calls cannot be recreated.
            await access.UnblockAsync(message.UserId);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Saga {SagaId}: Failed to restore voice access",
                message.SagaId);

            await messageClient.PublishAsync(
                new VoiceAccessRestorationFailed
                {
                    SagaId = message.SagaId,
                    UserId = message.UserId,
                    Reason = ex.Message
                },
                cancellationToken);

            return;
        }

        await messageClient.PublishAsync(
            new VoiceAccessRestored
            {
                SagaId = message.SagaId,
                UserId = message.UserId
            },
            cancellationToken);
    }
}
