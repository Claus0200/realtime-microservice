using RealtimeCommunication.Messaging;
using RealtimeCommunication.Messaging.Handlers;
using Shared.Contracts.Events;
using Xunit;

namespace RealtimeCommunication.Tests;

public class MessagePostedHandlerTests
{
    [Fact]
    public async Task Handle_PublishesResultEvent_WithCorrectMessageId()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        var client = new EasyNetQMessageClient();
        var logger = new LoggerFactory().CreateLogger<MessagePostedHandler>();
        var handler = new MessagePostedHandler(client, logger);

        var message = new MessagePostedEvent
        {
            MessageId = messageId,
            ChannelId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Content = "Hello world",
            PostedAt = DateTime.UtcNow
        };

        // Act
        await handler.Handle(message, CancellationToken.None);

        // Assert
        var published = client.SinglePublished<VoiceCommandCheckedEvent>();

        published.Should().NotBeNull();
        published.MessageId.Should().Be(messageId);
    }

    [Fact]
    public async Task Handler_CanConsume_MinimumValidContract()
    {
        // Arrange
        var client = new EasyNetQMessageClient();
        var logger = new LoggerFactory().CreateLogger<MessagePostedHandler>();
        var handler = new MessagePostedHandler(client, logger);

        var message = new MessagePostedEvent
        {
            MessageId = Guid.NewGuid(),
            ChannelId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Content = "Hello world",
            PostedAt = DateTime.UtcNow
        };

        // Act
        Func<Task> act = () =>
            handler.Handle(message, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        client.Published.Should().HaveCount(1);
    }


    [Fact]
    public async Task MessagePostedEvent_IsConsumed_AndResultEventPublished()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var capture = new MessageCapture<VoiceCommandCheckedEvent>(_factory.MessageClient);

        // Act
        await _factory.MessageClient.PublishAsync(new MessagePostedEvent
        {
            MessageId = messageId,
            ChannelId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Content = "Hello world",
            PostedAt = DateTime.UtcNow
        });

        
        var result = await capture.WaitForMessageAsync(TimeSpan.FromSeconds(5));

        // Assert
        result.MessageId.Should().Be(messageId);
    }
}