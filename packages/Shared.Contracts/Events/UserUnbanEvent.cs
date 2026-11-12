namespace Shared.Contracts.Events;

public class UserUnbanEvent
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}
