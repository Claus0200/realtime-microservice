
namespace Shared.Contracts.Commands;

public sealed class RemoveUserFromChannels
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}

public sealed class RestoreUserChannelMemberships
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}
