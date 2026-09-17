namespace RealtimeCommunication.Domain
{
    public class Participant
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTime JoinedAt { get; set; }

        public DateTime LeftAt { get; set; }

        public VoiceState VoiceState { get; set; } = new();

        public void SelfMute()
        {
            VoiceState.SelfMuted = true;
        }

        public void SelfUnmute()
        {
            VoiceState.SelfMuted = false;
        }

        public void SelfDeafen()
        {
            VoiceState.SelfDeafened = true;
        }

        public void SelfUndeafen()
        {
            VoiceState.SelfDeafened = false;
        }

        public void ServerMute()
        {
            VoiceState.ServerMuted = true;
        }

        public void RemoveServerMute()
        {
            VoiceState.ServerMuted = false;
        }

        public void ServerDeafen()
        {
            VoiceState.ServerDeafened = true;
        }

        public void RemoveServerDeafen()
        {
            VoiceState.ServerDeafened = false;
        }
    }
}
