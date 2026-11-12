namespace Shared.Contracts.Events;

public class UserBanEvent
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}
