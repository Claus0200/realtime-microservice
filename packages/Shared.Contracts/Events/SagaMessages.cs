namespace Shared.Contracts.Events;

// Commands: tell a service to do something
public class TerminateUserVoiceSessions
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}

public class RestoreUserVoiceAccess
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}

// Events: something happened
public class VoiceSessionsTerminated
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}

public class VoiceSessionTerminationFailed
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = null!;
}

public class VoiceAccessRestored
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
}

public class VoiceAccessRestorationFailed
{
    public Guid SagaId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = null!;
}
