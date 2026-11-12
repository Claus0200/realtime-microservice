using System.Text.Json.Serialization;

namespace RealtimeCommunication.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<UserBanSagaPhase>))]
public enum UserBanSagaPhase
{
    BanRequested,
    ChannelRemovalRequested,
    VoiceTerminationRequested,
    ChannelRestorationRequested,
    UnbanRequested,
    VoiceRestorationRequested,
    Completed,
    Failed,
    CompensationFailed
}