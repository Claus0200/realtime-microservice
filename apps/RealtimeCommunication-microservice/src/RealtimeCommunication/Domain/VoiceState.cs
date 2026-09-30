namespace RealtimeCommunication.Domain
{
    public class VoiceState
    {
        public bool SelfMuted { get; set; }

        public bool SelfDeafened { get; set; }

        public bool ServerMuted { get; set; }

        public bool ServerDeafened { get; set; }

        public bool isMuted()
        {
            return SelfMuted || ServerMuted;
        }

        public bool isDeafened()
        {
            return SelfDeafened || ServerDeafened;
        }
    }
}
