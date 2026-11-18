using HotChocolate;
using RealtimeCommunication.Services;
using RealtimeCommunication.Shared.Models;

namespace RealtimeCommunication.GraphQL;

public sealed record MyVoiceAccessResult(
    Guid UserId,
    bool IsBlocked);

public sealed class RealtimeQuery
{
    [GraphQLDescription("Checks whether the current user is blocked from voice chat.")]
    public async Task<MyVoiceAccessResult> GetMyVoiceAccess(
        IVoiceAccessStore voiceAccessStore,
        IHttpContextAccessor httpContextAccessor)
    {
        var subject = httpContextAccessor.HttpContext?
            .User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(subject, out var userId)
            || userId == Guid.Empty)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Invalid user identity.")
                    .SetCode("INVALID_USER_ID")
                    .Build());
        }

        var isBlocked =
            await voiceAccessStore.IsBlockedAsync(userId);

        return new MyVoiceAccessResult(userId, isBlocked);
    }

    public Task<IReadOnlyCollection<RealtimeSessionModel>>
    GetRealtimeSessions(
        IRealtimeSessionService service,
        CancellationToken cancellationToken)
    {
        return service.GetAllAsync(cancellationToken);
    }

    public Task<RealtimeSessionModel?> GetRealtimeSession(
        Guid id,
        IRealtimeSessionService service,
        CancellationToken cancellationToken)
    {
        return service.GetByIdAsync(id, cancellationToken);
    }
}