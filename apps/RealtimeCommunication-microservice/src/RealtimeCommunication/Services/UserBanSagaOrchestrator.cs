using RealtimeCommunication.Domain;
using RealtimeCommunication.Messaging;
using RealtimeCommunication.Repositories;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;

namespace RealtimeCommunication.Services;

public sealed class UserBanSagaOrchestrator(
    IMessageClient messages,
    IUserBanSagaRepository repository,
    UserBanSagaLocks locks) :
    IMessageHandler<InitiateUserBan>,
    IMessageHandler<UserBanEvent>,
    IMessageHandler<UserBanFailedEvent>,
    IMessageHandler<UserRemovedFromChannels>,
    IMessageHandler<UserRemovalFromChannelsFailed>,
    IMessageHandler<VoiceSessionsTerminated>,
    IMessageHandler<VoiceSessionTerminationFailed>,
    IMessageHandler<UserChannelMembershipsRestored>,
    IMessageHandler<UserChannelMembershipRestorationFailed>,
    IMessageHandler<UserUnbanEvent>,
    IMessageHandler<UserUnbanFailedEvent>,
    IMessageHandler<VoiceAccessRestored>,
    IMessageHandler<VoiceAccessRestorationFailed>
{
    private async Task<UserBanSagaState?> GetExpectedAsync(
        Guid sagaId,
        Guid userId,
        UserBanSagaPhase phase)
    {
        var state = await repository.GetById(sagaId);

        return state is not null &&
               state.UserId == userId &&
               state.Phase == phase
            ? state
            : null;
    }

    private static void Finish(
        UserBanSagaState state,
        bool compensated)
    {
        state.IsCompensating = false;
        state.IsCompleted = !compensated;
        state.IsFailed = compensated;

        state.Phase = compensated
            ? UserBanSagaPhase.Failed
            : UserBanSagaPhase.Completed;

        state.CompletedAt = DateTime.UtcNow;
    }

    private static void FailCompensation(
        UserBanSagaState state,
        string reason)
    {
        state.IsCompensating = false;
        state.IsFailed = true;
        state.CompensationFailed = true;
        state.Phase = UserBanSagaPhase.CompensationFailed;

        state.FailureReason =
            $"{state.FailureReason}; compensation failed: {reason}";

        state.CompletedAt = DateTime.UtcNow;
    }

    // STEP 1: Start Saga and request a user ban.
    public Task HandleAsync(
        InitiateUserBan message,
        CancellationToken ct) =>
        locks.RunAsync(message.SagaId, async () =>
        {
            if (message.UserId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var existing = await repository.GetById(message.SagaId);

            if (existing is not null)
            {
                if (existing.UserId != message.UserId)
                    throw new InvalidOperationException(
                        "SagaId belongs to another user");

                return;
            }

            await repository.Save(new UserBanSagaState
            {
                SagaId = message.SagaId,
                UserId = message.UserId,
                Phase = UserBanSagaPhase.BanRequested
            });

            await messages.PublishAsync(new BanUser
            {
                SagaId = message.SagaId,
                UserId = message.UserId
            }, ct);
        }, ct);

    // User Management successfully banned the user.
    // Continue by removing channel memberships.
    public Task HandleAsync(
        UserBanEvent message,
        CancellationToken ct) =>
        locks.RunAsync(message.SagaId, async () =>
        {
            var state = await GetExpectedAsync(
                message.SagaId,
                message.UserId,
                UserBanSagaPhase.BanRequested);

            if (state is null) return;

            state.UserBanned = true;
            state.Phase = UserBanSagaPhase.ChannelRemovalRequested;

            await repository.Save(state);

            await messages.PublishAsync(new RemoveUserFromChannels
            {
                SagaId = message.SagaId,
                UserId = message.UserId
            }, ct);
        }, ct);

    // Ban failed. Nothing has been completed yet,
    // so no previous step needs compensation.
    public Task HandleAsync(
        UserBanFailedEvent message,
        CancellationToken ct) =>
        locks.RunAsync(message.SagaId, async () =>
        {
            var state = await GetExpectedAsync(
                message.SagaId,
                message.UserId,
                UserBanSagaPhase.BanRequested);

            if (state is null) return;

            state.FailureReason = $"Ban failed: {message.Reason}";

            Finish(state, compensated: true);
            await repository.Save(state);
        }, ct);

    // STEP 2: Channel removal succeeded.
    // Continue by terminating active voice sessions.
    public Task HandleAsync(
        UserRemovedFromChannels message,
        CancellationToken ct) =>
        locks.RunAsync(message.SagaId, async () =>
        {
            var state = await GetExpectedAsync(
                message.SagaId,
                message.UserId,
                UserBanSagaPhase.ChannelRemovalRequested);

            if (state is null) return;

            state.UserRemovedFromChannels = true;
            state.Phase = UserBanSagaPhase.VoiceTerminationRequested;

            await repository.Save(state);

            await messages.PublishAsync(new TerminateUserVoiceSessions
            {
                SagaId = message.SagaId,
                UserId = message.UserId
            }, ct);
        }, ct);

    // Channel removal failed.
    // Restore potentially partially removed memberships.
    public Task HandleAsync(
        UserRemovalFromChannelsFailed message,
        CancellationToken ct) =>
        locks.RunAsync(message.SagaId, async () =>
        {
            var state = await GetExpectedAsync(
                message.SagaId,
                message.UserId,
                UserBanSagaPhase.ChannelRemovalRequested);

            if (state is null) return;

            state.FailureReason =
                $"Channel removal failed: {message.Reason}";

            state.IsCompensating = true;
            state.VoiceRestoreRequired = false;
            state.Phase = UserBanSagaPhase.ChannelRestorationRequested;

            await repository.Save(state);

            await messages.PublishAsync(
                new RestoreUserChannelMemberships
                {
                    SagaId = message.SagaId,
                    UserId = message.UserId
                }, ct);
        }, ct);

    // STEP 3: Voice sessions terminated successfully.
    // The complete user-ban Saga is now finished.
    public Task HandleAsync(
        VoiceSessionsTerminated message,
        CancellationToken ct) =>
        locks.RunAsync(message.SagaId, async () =>
        {
            var state = await GetExpectedAsync(
                message.SagaId,
                message.UserId,
                UserBanSagaPhase.VoiceTerminationRequested);

            if (state is null) return;

            state.VoiceSessionsTerminated = true;

            Finish(state, compensated: false);
            await repository.Save(state);
        }, ct);

    // Voice termination failed.
    // Begin compensating the earlier steps.
    public Task HandleAsync(
        VoiceSessionTerminationFailed message,
        CancellationToken ct) =>
        locks.RunAsync(message.SagaId, async () =>
        {
            var state = await GetExpectedAsync(
                message.SagaId,
                message.UserId,
                UserBanSagaPhase.VoiceTerminationRequested);

            if (state is null) return;

            state.FailureReason =
                $"Voice termination failed: {message.Reason}";

            state.IsCompensating = true;
            state.VoiceRestoreRequired = true;
            state.Phase = UserBanSagaPhase.ChannelRestorationRequested;

            await repository.Save(state);

            // Keep Redis blocked until after the user is unbanned.
            await messages.PublishAsync(
                new RestoreUserChannelMemberships
                {
                    SagaId = message.SagaId,
                    UserId = message.UserId
                }, ct);
        }, ct);

    // COMPENSATION 1: Memberships have been restored.
    // Undo the original ban.
    public Task HandleAsync(
        UserChannelMembershipsRestored message,
        CancellationToken ct) =>
        locks.RunAsync(message.SagaId, async () =>
        {
            var state = await GetExpectedAsync(
                message.SagaId,
                message.UserId,
                UserBanSagaPhase.ChannelRestorationRequested);

            if (state is null) return;

            state.ChannelsRestored = true;
            state.Phase = UserBanSagaPhase.UnbanRequested;

            await repository.Save(state);

            await messages.PublishAsync(new UnbanUser
            {
                SagaId = message.SagaId,
                UserId = message.UserId
            }, ct);
        }, ct);

    // Membership restoration failed.
    public Task HandleAsync(
        UserChannelMembershipRestorationFailed message,
        CancellationToken ct) =>
        locks.RunAsync(message.SagaId, async () =>
        {
            var state = await GetExpectedAsync(
                message.SagaId,
                message.UserId,
                UserBanSagaPhase.ChannelRestorationRequested);

            if (state is null) return;

            FailCompensation(
                state,
                $"Channel restoration: {message.Reason}");

            await repository.Save(state);
        }, ct);

    // COMPENSATION 2: User has been unbanned.
    public Task HandleAsync(
        UserUnbanEvent message,
        CancellationToken ct) =>
        locks.RunAsync(message.SagaId, async () =>
        {
            var state = await GetExpectedAsync(
                message.SagaId,
                message.UserId,
                UserBanSagaPhase.UnbanRequested);

            if (state is null) return;

            state.UserUnbanned = true;

            // If voice termination was never attempted,
            // there is no voice block to restore.
            if (!state.VoiceRestoreRequired)
            {
                Finish(state, compensated: true);
                await repository.Save(state);
                return;
            }

            state.Phase = UserBanSagaPhase.VoiceRestorationRequested;

            await repository.Save(state);

            await messages.PublishAsync(new RestoreUserVoiceAccess
            {
                SagaId = message.SagaId,
                UserId = message.UserId
            }, ct);
        }, ct);

    // Unbanning failed.
    public Task HandleAsync(
        UserUnbanFailedEvent message,
        CancellationToken ct) =>
        locks.RunAsync(message.SagaId, async () =>
        {
            var state = await GetExpectedAsync(
                message.SagaId,
                message.UserId,
                UserBanSagaPhase.UnbanRequested);

            if (state is null) return;

            FailCompensation(
                state,
                $"Unban: {message.Reason}");

            await repository.Save(state);
        }, ct);

    // COMPENSATION 3: Redis voice access has been restored.
    public Task HandleAsync(
        VoiceAccessRestored message,
        CancellationToken ct) =>
        locks.RunAsync(message.SagaId, async () =>
        {
            var state = await GetExpectedAsync(
                message.SagaId,
                message.UserId,
                UserBanSagaPhase.VoiceRestorationRequested);

            if (state is null) return;

            Finish(state, compensated: true);
            await repository.Save(state);
        }, ct);

    // Voice restoration failed.
    public Task HandleAsync(
        VoiceAccessRestorationFailed message,
        CancellationToken ct) =>
        locks.RunAsync(message.SagaId, async () =>
        {
            var state = await GetExpectedAsync(
                message.SagaId,
                message.UserId,
                UserBanSagaPhase.VoiceRestorationRequested);

            if (state is null) return;

            FailCompensation(
                state,
                $"Voice restoration: {message.Reason}");

            await repository.Save(state);
        }, ct);
}