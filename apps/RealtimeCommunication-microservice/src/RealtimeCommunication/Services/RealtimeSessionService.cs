using RealtimeCommunication.Shared.Models;
using RealtimeCommunication.Shared.Requests;

namespace RealtimeCommunication.Services
{
    public class RealtimeSessionService : IRealtimeSessionService
    {
        public Task<RealtimeSessionModel> CreateAsync(CreateRealtimeSessionRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<RealtimeSessionModel>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<RealtimeSessionModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<RealtimeSessionModel?> UpdateAsync(Guid id, UpdateRealtimeSessionRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
