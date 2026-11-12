namespace Shared.Contracts.Commands;

public class TerminateUserVoiceSessions
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}