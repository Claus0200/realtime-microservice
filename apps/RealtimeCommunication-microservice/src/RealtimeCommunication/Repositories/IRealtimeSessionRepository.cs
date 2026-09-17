using RealtimeCommunication.Domain;

namespace RealtimeCommunication.Repositories;

public interface IRealtimeSessionRepository
{
    Task<IReadOnlyCollection<RealtimeSession>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<RealtimeSession?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        RealtimeSession session,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        RealtimeSession session,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
