namespace SharedMessaging.Contracts.Customers;

public sealed record CustomerActivated(
    Guid CustomerId,
    string Email,
    DateTimeOffset ActivatedAt
);
