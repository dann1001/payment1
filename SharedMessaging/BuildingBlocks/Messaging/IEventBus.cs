namespace SharedMessaging.BuildingBlocks.Messaging;

public interface IEventBus : IDisposable
{
    Task PublishAsync<T>(
        T message,
        string? exchange = null,
        string? routingKey = null,
        IDictionary<string, object>? headers = null,
        CancellationToken cancellationToken = default);

    IDisposable Subscribe<T>(
        string queue,
        Func<T, MessageContext, CancellationToken, Task> handler,
        string? exchange = null,
        string? routingKey = null,
        ushort prefetchCount = 16,
        bool durableQueue = true);
}
