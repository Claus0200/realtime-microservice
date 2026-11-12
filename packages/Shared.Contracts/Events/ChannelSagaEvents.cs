
namespace Shared.Contracts.Events;

public sealed class UserRemovedFromChannels
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}

public sealed class UserRemovalFromChannelsFailed
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class UserChannelMembershipsRestored
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}

public sealed class UserChannelMembershipRestorationFailed
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
