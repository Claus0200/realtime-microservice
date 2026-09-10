using EasyNetQ;

namespace RealtimeCommunication.Messaging;

public sealed class EasyNetQMessageClient : IMessageClient, IAsyncDisposable
{
    private readonly IBus _bus;
    private readonly List<IAsyncDisposable> _subscriptions = [];

    public EasyNetQMessageClient(IBus bus)
    {
        _bus = bus;
    }

    public Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        return _bus.PubSub.PublishAsync(
            message,
            cancellationToken);
    }

    public async Task SubscribeAsync<TMessage>(
        string subscriptionId,
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _bus.PubSub.SubscribeAsync(
            subscriptionId,
            handler,
            _ => { },
            cancellationToken);

        _subscriptions.Add(subscription);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var subscription in _subscriptions)
        {
            await subscription.DisposeAsync();
        }
    }
}