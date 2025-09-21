namespace SharedMessaging.Contracts.Customers;

public sealed record CustomerEmailVerified(
    Guid CustomerId,
    DateTimeOffset VerifiedAtUtc
);
