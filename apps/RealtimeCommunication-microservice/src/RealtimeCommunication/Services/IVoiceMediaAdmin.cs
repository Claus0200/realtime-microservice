namespace RealtimeCommunication.Services;

public interface IVoiceMediaAdmin
{
    Task<int> DisconnectUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}