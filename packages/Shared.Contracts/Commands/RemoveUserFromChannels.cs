namespace Shared.Contracts.Commands;

public class RemoveUserFromChannels
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}
