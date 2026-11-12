
using Livekit.Server.Sdk.Dotnet;

namespace RealtimeCommunication.Services;

public sealed class LiveKitVoiceMediaAdmin(
    RoomServiceClient rooms,
    ILogger<LiveKitVoiceMediaAdmin> logger)
    : IVoiceMediaAdmin
{
    public async Task<int> DisconnectUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var identity = userId.ToString();
        var disconnected = 0;

        // Discover active rooms.
        var response = await rooms.ListRooms(
            new ListRoomsRequest());

        foreach (var room in response.Rooms)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Only manage Bizcord's voice rooms.
            if (!room.Name.StartsWith(
                    "voice-", StringComparison.Ordinal))
                continue;

            var participants = await rooms.ListParticipants(
                new ListParticipantsRequest
                {
                    Room = room.Name
                });

            // The user might not be in every room.
            if (!participants.Participants.Any(
                    p => p.Identity == identity))
                continue;

            await rooms.RemoveParticipant(
                new RoomParticipantIdentity
                {
                    Room = room.Name,
                    Identity = identity
                });

            disconnected++;

            logger.LogInformation(
                "Disconnected user {UserId} from {Room}",
                userId, room.Name);
        }

        return disconnected;
    }
}
