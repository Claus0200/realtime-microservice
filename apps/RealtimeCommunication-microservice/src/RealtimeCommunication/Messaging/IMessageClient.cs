namespace RealtimeCommunication.Messaging;

public interface IMessageClient
{
    Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default);

    Task SubscribeAsync<TMessage>(
        string subscriptionId,
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);
}