namespace RealtimeCommunication.Domain
{
    public class RealtimeSession
    {
        public Guid Id { get; set; }

        public Guid ChannelId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime EndedAt { get; set; }

        public RealtimeSessionStatus Status { get; set; }

        public List<Participant> Participants { get; set; } = new List<Participant>();

        public void Start()
        {
            Status = RealtimeSessionStatus.Active;
        }

        public void End()
        {
            Status = RealtimeSessionStatus.Ended;
        }

        public void Join(int UserId)
        {
            // Implementation for joining a session
            
        }

        public void Leave(int UserId)
        {
            // Implementation for leaving a session
            
        }
    }
}
