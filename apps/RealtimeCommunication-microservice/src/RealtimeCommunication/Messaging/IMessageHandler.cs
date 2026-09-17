namespace RealtimeCommunication.Messaging;

public interface IMessageHandler<in TMessage>
{
    Task HandleAsync(
        TMessage message,
        CancellationToken cancellationToken);
}
