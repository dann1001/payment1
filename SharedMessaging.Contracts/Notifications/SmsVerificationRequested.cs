namespace SharedMessaging.Contracts.Notifications;

public sealed record SmsVerificationRequested(
    Guid CustomerId,
    string PhoneNumber,
    string Code
);
