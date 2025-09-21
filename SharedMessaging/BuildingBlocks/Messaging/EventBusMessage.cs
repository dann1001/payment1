namespace SharedMessaging.BuildingBlocks.Messaging;

public abstract record EventBusMessage
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString("N");
    public string? CorrelationId { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
