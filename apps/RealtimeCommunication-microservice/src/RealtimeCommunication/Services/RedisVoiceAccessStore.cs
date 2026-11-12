
using StackExchange.Redis;

namespace RealtimeCommunication.Services;

public sealed class RedisVoiceAccessStore(
    IConnectionMultiplexer redis) : IVoiceAccessStore
{
    private static string Key(Guid userId)
        => $"voice:blocked:{userId:N}";

    public async Task BlockAsync(Guid userId)
    {
        await redis.GetDatabase()
            .StringSetAsync(Key(userId), "1");
    }

    public async Task UnblockAsync(Guid userId)
    {
        await redis.GetDatabase()
            .KeyDeleteAsync(Key(userId));
    }

    public async Task<bool> IsBlockedAsync(Guid userId)
    {
        return await redis.GetDatabase()
            .KeyExistsAsync(Key(userId));
    }
}
