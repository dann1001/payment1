using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedMessaging.BuildingBlocks.Messaging;

namespace SharedMessaging.BuildingBlocks.Messaging.RabbitMQ;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqEventBus(
        this IServiceCollection services,
        IConfiguration config,
        string sectionName = "RabbitMq")
    {
        services.Configure<RabbitMqOptions>(config.GetSection(sectionName));
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        return services;
    }

    public static IServiceCollection AddIntegrationEventHandlers(
        this IServiceCollection services,
        params System.Reflection.Assembly[] assemblies)
    {
        var handlerType = typeof(IIntegrationEventHandler<>);
        foreach (var asm in assemblies)
        {
            var types = asm.GetTypes().Where(t =>
                !t.IsAbstract && !t.IsInterface &&
                t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType));

            foreach (var t in types)
                services.AddScoped(t);
        }

        return services;
    }
}
