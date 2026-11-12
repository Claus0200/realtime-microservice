namespace Shared.Contracts.Events;

public class VoiceAccessRestored
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}