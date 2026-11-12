
using Livekit.Server.Sdk.Dotnet;

namespace RealtimeCommunication.Services;

public sealed class LiveKitTokenService(
    IConfiguration config,
    IVoiceAccessStore access)
{
    public async Task<string> CreateAsync(
        Guid channelId,
        Guid userId)
    {
        if (await access.IsBlockedAsync(userId))
            throw new UnauthorizedAccessException(
                "User is blocked from voice chat");

        if (config.GetValue<bool>("Voice:SimulateTokenFailure"))
            throw new InvalidOperationException(
                "Simulated token failure");

        var key = config["LiveKit:ApiKey"]
            ?? throw new InvalidOperationException(
                "LiveKit key missing");

        var secret = config["LiveKit:ApiSecret"]
            ?? throw new InvalidOperationException(
                "LiveKit secret missing");

        return new AccessToken(key, secret)
            .WithIdentity(userId.ToString())
            .WithGrants(new VideoGrants
            {
                RoomJoin = true,
                Room = $"voice-{channelId:N}",
                CanPublish = true,
                CanSubscribe = true
            })
            .WithTtl(TimeSpan.FromMinutes(5))
            .ToJwt();
    }
}
