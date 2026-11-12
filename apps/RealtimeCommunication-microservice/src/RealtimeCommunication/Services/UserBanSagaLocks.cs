using System.Collections.Concurrent;

namespace RealtimeCommunication.Services;

public sealed class UserBanSagaLocks
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task RunAsync(
        Guid sagaId,
        Func<Task> action,
        CancellationToken ct)
    {
        if (sagaId == Guid.Empty)
            throw new ArgumentException(
                "SagaId is required", nameof(sagaId));

        var gate = _locks.GetOrAdd(
            sagaId, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(ct);

        try
        {
            await action();
        }
        finally
        {
            gate.Release();
        }
    }
}