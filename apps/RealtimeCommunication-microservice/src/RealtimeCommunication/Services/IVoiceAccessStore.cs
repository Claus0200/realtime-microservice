namespace RealtimeCommunication.Services;

public interface IVoiceAccessStore
{
    Task BlockAsync(Guid userId);
    Task UnblockAsync(Guid userId);
    Task<bool> IsBlockedAsync(Guid userId);
}