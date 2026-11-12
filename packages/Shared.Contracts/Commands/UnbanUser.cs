namespace Shared.Contracts.Commands;

public class UnbanUser
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}
