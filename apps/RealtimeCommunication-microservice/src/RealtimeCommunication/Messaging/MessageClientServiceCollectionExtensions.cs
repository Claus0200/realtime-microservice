using EasyNetQ;

namespace RealtimeCommunication.Messaging;

public static class MessageClientServiceCollectionExtensions
{
    public static IServiceCollection AddMessageClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("RabbitMq")
            ?? throw new InvalidOperationException(
                "RabbitMQ connection string is not configured.");

        services.AddEasyNetQ(connectionString);

        services.AddSingleton<IMessageClient, EasyNetQMessageClient>();

        return services;
    }
}