namespace Shared.Contracts.Events;

public class MessageBroadcastedEvent
{
    public Guid MessageId { get; set; }
    public Guid ChannelId { get; set; }
    public DateTime BroadcastedAt { get; set; }
}
