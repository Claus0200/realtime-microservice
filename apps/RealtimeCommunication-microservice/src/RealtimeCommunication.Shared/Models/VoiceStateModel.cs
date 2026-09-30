namespace RealtimeCommunication.Shared.Models;

public sealed record VoiceStateModel
{
    public bool SelfMuted { get; init; }
    public bool SelfDeafened { get; init; }
    public bool ServerMuted { get; init; }
    public bool ServerDeafened { get; init; }
}
