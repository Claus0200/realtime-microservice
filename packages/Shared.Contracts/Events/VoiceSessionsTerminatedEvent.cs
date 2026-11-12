namespace Shared.Contracts.Events;

public class VoiceSessionsTerminated
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}