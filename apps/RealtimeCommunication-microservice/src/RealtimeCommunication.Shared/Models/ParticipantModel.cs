namespace RealtimeCommunication.Shared.Models;

public sealed record ParticipantModel
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset JoinedAt { get; init; }
    public DateTimeOffset? LeftAt { get; init; }

    public VoiceStateModel VoiceState { get; init; } = new();
}
