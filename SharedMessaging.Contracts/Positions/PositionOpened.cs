namespace SharedMessaging.Contracts.Positions;

public sealed record PositionOpened(
    Guid PositionId,
    string Symbol,
    decimal Quantity,
    decimal Price,
    DateTimeOffset Timestamp
);

