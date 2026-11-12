using RealtimeCommunication.Domain;
using RealtimeCommunication.Repositories;
using RealtimeCommunication.Services;
using RealtimeCommunication.Tests.Fakes;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;

namespace RealtimeCommunication.Tests;

public sealed class UserBanSagaOrchestratorTests
{
    private static readonly Guid SagaId =
        Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");

    private static readonly Guid UserId =
        Guid.Parse("22222222-2222-4222-8222-222222222222");

    private static (
        UserBanSagaOrchestrator Saga,
        FakeMessageClient Bus,
        InMemoryUserBanSagaRepository Repo) Create()
    {
        var bus = new FakeMessageClient();
        var repo = new InMemoryUserBanSagaRepository();

        var saga = new UserBanSagaOrchestrator(
            bus,
            repo,
            new UserBanSagaLocks());

        return (saga, bus, repo);
    }

    [Fact]
    public async Task HappyPath_CompletesAllThreeSteps()
    {
        var (saga, bus, repo) = Create();

        await saga.HandleAsync(new InitiateUserBan
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        Assert.Single(bus.Published.OfType<BanUser>());

        await saga.HandleAsync(new UserBanEvent
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        Assert.Single(
            bus.Published.OfType<RemoveUserFromChannels>());

        await saga.HandleAsync(new UserRemovedFromChannels
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        Assert.Single(
            bus.Published.OfType<TerminateUserVoiceSessions>());

        await saga.HandleAsync(new VoiceSessionsTerminated
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        var state = await repo.GetById(SagaId);

        Assert.NotNull(state);
        Assert.True(state.IsCompleted);
        Assert.Equal(UserBanSagaPhase.Completed, state.Phase);
    }

    [Fact]
    public async Task BanFailure_EndsSagaWithoutCompensation()
    {
        var (saga, bus, repo) = Create();

        await saga.HandleAsync(new InitiateUserBan
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        await saga.HandleAsync(new UserBanFailedEvent
        {
            SagaId = SagaId,
            UserId = UserId,
            Reason = "Ban rejected"
        }, default);

        var state = await repo.GetById(SagaId);

        Assert.NotNull(state);
        Assert.True(state.IsFailed);
        Assert.Equal(UserBanSagaPhase.Failed, state.Phase);
        Assert.Empty(bus.Published.OfType<UnbanUser>());
    }

    [Fact]
    public async Task ChannelFailure_RestoresChannelsThenUnbans()
    {
        var (saga, bus, repo) = Create();

        await saga.HandleAsync(new InitiateUserBan
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        await saga.HandleAsync(new UserBanEvent
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        await saga.HandleAsync(new UserRemovalFromChannelsFailed
        {
            SagaId = SagaId,
            UserId = UserId,
            Reason = "Database unavailable"
        }, default);

        Assert.Single(
            bus.Published.OfType<RestoreUserChannelMemberships>());

        await saga.HandleAsync(new UserChannelMembershipsRestored
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        Assert.Single(bus.Published.OfType<UnbanUser>());

        await saga.HandleAsync(new UserUnbanEvent
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        var state = await repo.GetById(SagaId);

        Assert.NotNull(state);
        Assert.True(state.IsFailed);
        Assert.False(state.IsCompensating);
        Assert.False(state.CompensationFailed);

        Assert.Empty(
            bus.Published.OfType<RestoreUserVoiceAccess>());
    }

    [Fact]
    public async Task VoiceFailure_ExecutesAllCompensationSteps()
    {
        var (saga, bus, repo) = Create();

        await saga.HandleAsync(new InitiateUserBan
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        await saga.HandleAsync(new UserBanEvent
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        await saga.HandleAsync(new UserRemovedFromChannels
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        await saga.HandleAsync(new VoiceSessionTerminationFailed
        {
            SagaId = SagaId,
            UserId = UserId,
            Reason = "LiveKit offline"
        }, default);

        Assert.Single(
            bus.Published.OfType<RestoreUserChannelMemberships>());

        await saga.HandleAsync(new UserChannelMembershipsRestored
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        Assert.Single(bus.Published.OfType<UnbanUser>());

        await saga.HandleAsync(new UserUnbanEvent
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        Assert.Single(
            bus.Published.OfType<RestoreUserVoiceAccess>());

        await saga.HandleAsync(new VoiceAccessRestored
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        var state = await repo.GetById(SagaId);

        Assert.NotNull(state);
        Assert.True(state.IsFailed);
        Assert.False(state.CompensationFailed);
        Assert.Equal(UserBanSagaPhase.Failed, state.Phase);
    }

    [Fact]
    public async Task DuplicateEvent_DoesNotRepeatCommand()
    {
        var (saga, bus, _) = Create();

        await saga.HandleAsync(new InitiateUserBan
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        var result = new UserBanEvent
        {
            SagaId = SagaId,
            UserId = UserId
        };

        await saga.HandleAsync(result, default);
        await saga.HandleAsync(result, default);

        Assert.Single(
            bus.Published.OfType<RemoveUserFromChannels>());
    }

    [Fact]
    public async Task CompensationFailure_IsRecorded()
    {
        var (saga, _, repo) = Create();

        await saga.HandleAsync(new InitiateUserBan
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        await saga.HandleAsync(new UserBanEvent
        {
            SagaId = SagaId,
            UserId = UserId
        }, default);

        await saga.HandleAsync(new UserRemovalFromChannelsFailed
        {
            SagaId = SagaId,
            UserId = UserId,
            Reason = "Partial removal"
        }, default);

        await saga.HandleAsync(new UserChannelMembershipRestorationFailed
        {
            SagaId = SagaId,
            UserId = UserId,
            Reason = "Restoration unavailable"
        }, default);

        var state = await repo.GetById(SagaId);

        Assert.NotNull(state);
        Assert.True(state.CompensationFailed);

        Assert.Equal(
            UserBanSagaPhase.CompensationFailed,
            state.Phase);
    }
}