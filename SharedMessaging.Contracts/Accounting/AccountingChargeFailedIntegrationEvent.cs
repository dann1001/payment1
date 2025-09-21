// SharedMessaging.Contracts/Accounting/AccountingChargeFailedIntegrationEvent.cs
namespace SharedMessaging.Contracts.Accounting;

public sealed record AccountingChargeFailedIntegrationEvent(
    Guid PublicId, int CustomerId, decimal Amount, string? FailureReason);
