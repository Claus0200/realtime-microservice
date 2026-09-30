using EasyNetQ;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using RealtimeCommunication.Messaging;
using RealtimeCommunication.Messaging.Handlers;
using Shared.Contracts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RealtimeCommunication.Tests;

public class MessagePostedHandlerTests
{
    [Fact]
    public async Task Handle_PublishesResultEvent_WithCorrectMessageId()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        var client = new FakeMessageClient();

        var handler = new MessagePostedHandler(
            client,
            NullLogger<MessagePostedHandler>.Instance);

        var message = new MessagePostedEvent
        {
            MessageId = messageId,
            ChannelId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Content = "Hello world",
            PostedAt = DateTime.UtcNow
        };

        // Act
        await handler.HandleAsync(
            message,
            CancellationToken.None);

        // Assert
        var published =
            client.SinglePublished<VoiceCommandCheckedEvent>();

        published.Should().NotBeNull();

        published!.MessageId
            .Should()
            .Be(messageId);
    }

    [Fact]
    public async Task Handler_CanConsume_MinimumValidContract()
    {
        // Arrange
        var client = new FakeMessageClient();

        var handler = new MessagePostedHandler(
            client,
            NullLogger<MessagePostedHandler>.Instance);

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
            handler.HandleAsync(
                message,
                CancellationToken.None);

        // Assert
        await act
            .Should()
            .NotThrowAsync();

        client.Published
            .Should()
            .HaveCount(1);
    }

    [Fact]
    public async Task Handle_MuteCommand_PublishesDetectedVoiceCommand()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var client = new FakeMessageClient();

        var handler = new MessagePostedHandler(
            client,
            NullLogger<MessagePostedHandler>.Instance);

        var message = new MessagePostedEvent
        {
            MessageId = messageId,
            ChannelId = channelId,
            AuthorId = authorId,
            Content = "/mute",
            PostedAt = DateTime.UtcNow
        };

        // Act
        await handler.HandleAsync(
            message,
            CancellationToken.None);

        // Assert
        var published =
            client.SinglePublished<VoiceCommandCheckedEvent>();

        published.Should().NotBeNull();

        published!.MessageId
            .Should()
            .Be(messageId);

        published.ChannelId
            .Should()
            .Be(channelId);

        published.AuthorId
            .Should()
            .Be(authorId);

        published.VoiceCommandDetected
            .Should()
            .BeTrue();

        published.Command
            .Should()
            .Be("mute");
    }
}


public class MessagePostedHandlerIntegrationTests
{
    [Fact]
    public async Task MessagePostedEvent_IsConsumed_AndResultEventPublished()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:RabbitMq"] =
                    "host=localhost;username=bizcord;password=bizcord",

                ["Messaging:SubscriptionPrefix"] =
                    $"realtime-communication-test-{Guid.NewGuid():N}"
            });

        builder.Services.AddMessageClient(
            builder.Configuration);

        builder.Services.AddMessageHandlers(
            typeof(MessagePostedHandler).Assembly);

        using var host = builder.Build();

        await host.StartAsync();

        var messageClient =
            host.Services.GetRequiredService<IMessageClient>();

        var messageId = Guid.NewGuid();

        var capture =
            new MessageCapture<VoiceCommandCheckedEvent>(
                messageClient);

        var message = new MessagePostedEvent
        {
            MessageId = messageId,
            ChannelId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Content = "Hello world",
            PostedAt = DateTime.UtcNow
        };

        // Act
        await messageClient.PublishAsync(message);

        var result =
            await capture.WaitForMessageAsync(
                TimeSpan.FromSeconds(5));

        // Assert
        result.MessageId
            .Should()
            .Be(messageId);

        await host.StopAsync();
    }
}


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


public sealed class MessageCapture<TMessage>
{
    private readonly TaskCompletionSource<TMessage> _messageSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public MessageCapture(
        IMessageClient messageClient)
    {
        var subscriptionId =
            $"test-capture-{typeof(TMessage).Name}-{Guid.NewGuid():N}";

        messageClient
            .SubscribeAsync<TMessage>(
                subscriptionId,
                (message, cancellationToken) =>
                {
                    _messageSource.TrySetResult(message);

                    return Task.CompletedTask;
                })
            .GetAwaiter()
            .GetResult();
    }

    public async Task<TMessage> WaitForMessageAsync(
        TimeSpan timeout)
    {
        using var cancellationTokenSource =
            new CancellationTokenSource(timeout);

        return await _messageSource.Task
            .WaitAsync(cancellationTokenSource.Token);
    }
}