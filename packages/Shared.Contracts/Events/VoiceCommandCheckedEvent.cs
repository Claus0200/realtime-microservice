namespace Shared.Contracts.Events;

public class VoiceCommandCheckedEvent
{
    public Guid MessageId { get; set; }

    public Guid ChannelId { get; set; }

    public Guid AuthorId { get; set; }

    public bool VoiceCommandDetected { get; set; }

    public string? Command { get; set; }

    public DateTime ProcessedAt { get; set; }
}