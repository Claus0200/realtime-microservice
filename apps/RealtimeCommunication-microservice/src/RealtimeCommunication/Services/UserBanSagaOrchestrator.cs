using RealtimeCommunication.Repositories;
using RealtimeCommunication.Messaging;
using RealtimeCommunication.Domain;
using Shared.Contracts.Events;
using Shared.Contracts.Commands;

namespace RealtimeCommunication.Services;

public class UserBanSagaOrchestrator :
    IMessageHandler<InitiateUserBan>,
    IMessageHandler<UserBanEvent>,
    IMessageHandler<UserBanFailedEvent>,
    IMessageHandler<VoiceSessionsTerminated>,
    IMessageHandler<VoiceSessionTerminationFailed>,
    IMessageHandler<UserUnbanEvent>,
    IMessageHandler<UserUnbanFailedEvent>
{
    private readonly IMessageClient _messageClient;
    private readonly IUserBanSagaRepository _sagaRepo;

    public UserBanSagaOrchestrator(
        IMessageClient messageClient,
        IUserBanSagaRepository sagaRepo)
    {
        _messageClient = messageClient;
        _sagaRepo = sagaRepo;
    }

    // Initiate user ban
    public async Task HandleAsync(
        InitiateUserBan message,
        CancellationToken ct)
    {
        var state = new UserBanSagaState
        {
            SagaId = message.SagaId,
            UserId = message.UserId
        };

        await _sagaRepo.Save(state);

        await _messageClient.PublishAsync(new BanUser
        {
            SagaId = message.SagaId,
            UserId = message.UserId
        });
    }

    // User banned
    public async Task HandleAsync(
        UserBanEvent message,
        CancellationToken ct)
    {
        var state = await _sagaRepo.GetById(message.SagaId);
        if (state is null || state.IsCompleted || state.IsFailed)
            return;

        state.UserBanned = true;
        await _sagaRepo.Save(state);
    }

    // User ban failed
    public async Task HandleAsync(
        UserBanFailedEvent message,
        CancellationToken ct)
    {
        var state = await _sagaRepo.GetById(message.SagaId);
        if (state is null)
            return;

        state.IsFailed = true;
        state.FailureReason = message.Reason;
        state.CompletedAt = DateTime.UtcNow;

        await _sagaRepo.Save(state);
    }

    // Voice session terminated
    public async Task HandleAsync(
        VoiceSessionsTerminated message,
        CancellationToken ct)
    {
        var state = await _sagaRepo.GetById(message.SagaId);
        if (state is null || state.IsCompleted || state.IsFailed)
            return;

        state.VoiceSessionsTerminated = true;
        state.IsCompleted = true;
        state.CompletedAt = DateTime.UtcNow;

        await _sagaRepo.Save(state);
    }

    // Voice session terminated failed
    public async Task HandleAsync(
        VoiceSessionTerminationFailed message,
        CancellationToken ct)
    {
        var state = await _sagaRepo.GetById(message.SagaId);
        if (state is null)
            return;

        state.IsCompensating = true;
        state.FailureReason = message.Reason;

        await _sagaRepo.Save(state);
    }

    // User unbanned
    public async Task HandleAsync(
        UserUnbanEvent message,
        CancellationToken ct)
    {
        var state = await _sagaRepo.GetById(message.SagaId);
        if (state is null)
            return;

        state.UserUnbanned = true;
        state.IsCompensating = false;
        state.IsFailed = true;
        state.CompletedAt = DateTime.UtcNow;

        await _sagaRepo.Save(state);
    }

    // User unbanned failed
    public async Task HandleAsync(
        UserUnbanFailedEvent message,
        CancellationToken ct)
    {
        var state = await _sagaRepo.GetById(message.SagaId);
        if (state is null)
            return;

        state.IsFailed = true;
        state.FailureReason =
            $"Unban failed: {message.Reason}";
        state.CompletedAt = DateTime.UtcNow;

        await _sagaRepo.Save(state);
    }
}
