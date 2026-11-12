namespace Shared.Contracts.Commands;

public class InitiateUserBan
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}