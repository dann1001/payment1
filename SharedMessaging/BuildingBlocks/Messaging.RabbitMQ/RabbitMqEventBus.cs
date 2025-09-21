using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedMessaging.BuildingBlocks.Messaging;
using System.Text.Json;
// Force top-level external namespaces (prevents collision with your .RabbitMQ namespace)
using Rmq = global::RabbitMQ.Client;
using RmqEvents = global::RabbitMQ.Client.Events;

namespace SharedMessaging.BuildingBlocks.Messaging.RabbitMQ;

public sealed class RabbitMqEventBus : IEventBus
{
    private readonly RabbitMqOptions _options;
    private readonly JsonSerializerOptions _json;
    private readonly Rmq.IConnection _connection;
    private readonly Rmq.IModel _channel;
    private readonly List<IDisposable> _subscriptions = new();

    public RabbitMqEventBus(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;

        _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var factory = new Rmq.ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // existing exchange
        _channel.ExchangeDeclare(_options.ExchangeName, _options.ExchangeType, durable: true, autoDelete: false);

        // declare DLX only if configured
        if (!string.IsNullOrWhiteSpace(_options.DeadLetterExchange))
        {
            _channel.ExchangeDeclare(_options.DeadLetterExchange, "fanout", durable: true, autoDelete: false);
        }
    }

    public async Task PublishAsync<T>(
        T message,
        string? exchange = null,
        string? routingKey = null,
        IDictionary<string, object>? headers = null,
        CancellationToken cancellationToken = default)
    {
        var ex = exchange ?? _options.ExchangeName;
        var rk = routingKey ?? RoutingKeyConventions.For<T>();

        var body = JsonSerializer.SerializeToUtf8Bytes(message!, _json);

        var props = _channel.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2; // persistent
        props.MessageId = TryGetMessageId(message);
        props.CorrelationId = TryGetCorrelationId(message);
        props.Type = typeof(T).FullName;

        if (headers is not null && headers.Count > 0)
            props.Headers = new Dictionary<string, object>(headers);

        _channel.BasicPublish(exchange: ex, routingKey: rk, basicProperties: props, body: body);
        await Task.CompletedTask;
    }

    public IDisposable Subscribe<T>(
     string queue,
     Func<T, MessageContext, CancellationToken, Task> handler,
     string? exchange = null,
     string? routingKey = null,
     ushort prefetchCount = 16,
     bool durableQueue = true)
    {
        var ex = exchange ?? _options.ExchangeName;
        var rk = routingKey ?? RoutingKeyConventions.For<T>();
        var qName = string.IsNullOrWhiteSpace(queue) ? $"{_options.QueuePrefix}.{rk}" : queue;

        // Set DLX args if configured
        IDictionary<string, object>? args = null;
        if (!string.IsNullOrWhiteSpace(_options.DeadLetterExchange))
        {
            args = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = _options.DeadLetterExchange
            };
        }

        // Always declare the queue (idempotent if same params)
        _channel.QueueDeclare(qName, durable: durableQueue, exclusive: false, autoDelete: false, arguments: args);

        // Ensure binding
        _channel.QueueBind(qName, ex, rk);

        // Prefetch
        _channel.BasicQos(0, prefetchCount, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var msg = JsonSerializer.Deserialize<T>(ea.Body.ToArray(), _json)!;
                var ctx = new MessageContext(
                    DeliveryTag: ea.DeliveryTag,
                    RoutingKey: ea.RoutingKey,
                    Redelivered: ea.Redelivered,
                    Headers: ea.BasicProperties?.Headers != null
                        ? new Dictionary<string, object>(ea.BasicProperties.Headers)
                        : null
                );

                await handler(msg, ctx, CancellationToken.None);
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch
            {
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        var tag = _channel.BasicConsume(qName, autoAck: false, consumer: consumer);

        var subscription = new Subscription(() =>
        {
            try { _channel.BasicCancel(tag); } catch { /* ignore */ }
        });

        _subscriptions.Add(subscription);
        return subscription;
    }


    public void Dispose()
    {
        foreach (var s in _subscriptions) s.Dispose();
        try { _channel?.Close(); } catch { }
        try { _connection?.Close(); } catch { }
        _channel?.Dispose();
        _connection?.Dispose();
    }

    private static string TryGetMessageId<T>(T message) =>
        (message as EventBusMessage)?.MessageId ?? Guid.NewGuid().ToString("N");

    private static string? TryGetCorrelationId<T>(T message) =>
        (message as EventBusMessage)?.CorrelationId ?? null;

    private sealed class Subscription : IDisposable
    {
        private readonly Action _onDispose;
        private bool _disposed;
        public Subscription(Action onDispose) => _onDispose = onDispose;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _onDispose();
        }
    }
}
