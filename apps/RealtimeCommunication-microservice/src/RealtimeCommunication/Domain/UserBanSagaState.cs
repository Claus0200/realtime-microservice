namespace RealtimeCommunication.Domain;

public class UserBanSagaState
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }

    public bool UserBanned { get; set; }
    public bool UserRemovedFromChannels { get; set; }
    public bool VoiceSessionsTerminated { get; set; }

    public bool ChannelsRestored { get; set; }
    public bool UserUnbanned { get; set; }

    public bool IsCompensating { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }

    public string? FailureReason { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
