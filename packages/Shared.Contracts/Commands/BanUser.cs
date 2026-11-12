namespace Shared.Contracts.Commands;

public class BanUser
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}
