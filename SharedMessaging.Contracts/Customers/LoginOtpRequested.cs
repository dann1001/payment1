namespace SharedMessaging.Contracts.Customers;

public sealed record LoginOtpRequested(Guid CustomerId, string Email, string Code, DateTimeOffset ExpiresAtUtc);
