using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RealtimeCommunication.Messaging;

public static class MessageHandlerServiceCollectionExtensions
{
    public static IServiceCollection AddMessageHandlers(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            assemblies = new[] { Assembly.GetCallingAssembly() };
        }

        var registrations = assemblies
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type =>
                !type.IsAbstract &&
                !type.IsInterface)
            .SelectMany(type => type.ImplementedInterfaces
                .Where(@interface =>
                    @interface.IsGenericType &&
                    @interface.GetGenericTypeDefinition()
                        == typeof(IMessageHandler<>))
                .Select(@interface => new
                {
                    HandlerType = type.AsType(),
                    HandlerInterface = @interface,
                    MessageType = @interface.GenericTypeArguments[0]
                }))
            .ToArray();

        foreach (var registration in registrations)
        {
            services.AddScoped(
                registration.HandlerInterface,
                registration.HandlerType);
        }

        foreach (var messageType in registrations
                     .Select(registration => registration.MessageType)
                     .Distinct())
        {
            services.AddSingleton(
                typeof(IHostedService),
                serviceProvider =>
                {
                    var hostedServiceType =
                        typeof(MessageHandlerBackgroundService<>)
                            .MakeGenericType(messageType);

                    return (IHostedService)
                        ActivatorUtilities.CreateInstance(
                            serviceProvider,
                            hostedServiceType);
                });
        }

        return services;
    }
}
