namespace RealtimeCommunication.Domain;

public class UserBanSagaState
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }

    public UserBanSagaPhase Phase { get; set; }
        = UserBanSagaPhase.BanRequested;

    public bool UserBanned { get; set; }
    public bool UserRemovedFromChannels { get; set; }
    public bool VoiceSessionsTerminated { get; set; }

    public bool ChannelsRestored { get; set; }
    public bool UserUnbanned { get; set; }

    // Voice termination may have blocked Redis
    // even when disconnecting from LiveKit failed.
    public bool VoiceRestoreRequired { get; set; }

    public bool IsCompensating { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }
    public bool CompensationFailed { get; set; }

    public string? FailureReason { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}