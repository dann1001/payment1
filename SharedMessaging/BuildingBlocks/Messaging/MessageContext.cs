namespace SharedMessaging.BuildingBlocks.Messaging;

public sealed record MessageContext(
    ulong DeliveryTag,
    string RoutingKey,
    bool Redelivered,
    IReadOnlyDictionary<string, object>? Headers
);
