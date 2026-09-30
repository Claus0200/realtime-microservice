using RealtimeCommunication.Messaging;

namespace RealtimeCommunication.Tests.Fakes;

public sealed class FakeMessageClient : IMessageClient
{
    public List<object> Published { get; } = [];

    public Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        Published.Add(message!);

        return Task.CompletedTask;
    }

    public Task SubscribeAsync<TMessage>(
        string subscriptionId,
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public TMessage? SinglePublished<TMessage>()
    {
        return Published
            .OfType<TMessage>()
            .SingleOrDefault();
    }
}