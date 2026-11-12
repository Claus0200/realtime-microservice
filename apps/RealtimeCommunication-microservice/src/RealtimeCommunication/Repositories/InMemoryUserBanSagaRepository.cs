using System.Collections.Concurrent;
using RealtimeCommunication.Domain;

namespace RealtimeCommunication.Repositories;

public class InMemoryUserBanSagaRepository : IUserBanSagaRepository
{
    private readonly ConcurrentDictionary<Guid, UserBanSagaState> _states = new();

    public Task<UserBanSagaState?> GetById(Guid sagaId)
    {
        _states.TryGetValue(sagaId, out var state);
        return Task.FromResult(state);
    }

    public Task Save(UserBanSagaState state)
    {
        _states[state.SagaId] = state;
        return Task.CompletedTask;
    }
}