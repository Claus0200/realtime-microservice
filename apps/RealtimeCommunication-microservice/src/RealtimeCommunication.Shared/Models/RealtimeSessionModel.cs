namespace RealtimeCommunication.Shared.Models;

public sealed record RealtimeSessionModel
{
    public Guid Id { get; init; }
    public Guid ChannelId { get; init; }
    public RealtimeSessionStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }

    public IReadOnlyCollection<ParticipantModel> Participants { get; init; }
        = Array.Empty<ParticipantModel>();
}
