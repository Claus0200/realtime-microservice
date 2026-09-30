namespace RealtimeCommunication.Domain
{
    public class ParticipantConnection
    {
        public Guid ConnectionId { get; set; }

        public DateTime ConnectionAt { get; set; }

        public DateTime DisconnectedAt { get; set; }

        public ConnectionState State { get; set; }

        public void Connect()
        {
            State = ConnectionState.Connected;
            // Implementation for connecting a participant
        }

        public void Disconnect()
        {
            State = ConnectionState.Disconnected;
            // Implementation for connecting a participant
        }

    }
}
