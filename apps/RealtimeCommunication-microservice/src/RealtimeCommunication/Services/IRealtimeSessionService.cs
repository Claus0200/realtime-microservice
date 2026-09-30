using RealtimeCommunication.Shared.Models;
using RealtimeCommunication.Shared.Requests;

namespace RealtimeCommunication.Services;

public interface IRealtimeSessionService
{
    Task<IReadOnlyCollection<RealtimeSessionModel>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<RealtimeSessionModel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<RealtimeSessionModel> CreateAsync(
        CreateRealtimeSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<RealtimeSessionModel?> UpdateAsync(
        Guid id,
        UpdateRealtimeSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
