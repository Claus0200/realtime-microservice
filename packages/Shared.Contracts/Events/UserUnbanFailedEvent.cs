namespace Shared.Contracts.Events;

public class UserUnbanFailedEvent
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = null!;
}