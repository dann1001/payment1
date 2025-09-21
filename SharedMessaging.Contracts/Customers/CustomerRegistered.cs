namespace SharedMessaging.Contracts.Customers;

public sealed record CustomerRegistered(
    Guid CustomerId,
    string Email,
    string PhoneNumber,
    DateTimeOffset RegisteredAtUtc
);
