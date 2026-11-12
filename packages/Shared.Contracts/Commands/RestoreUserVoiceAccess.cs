namespace Shared.Contracts.Commands;

public class RestoreUserVoiceAccess
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}