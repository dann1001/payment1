// SharedMessaging.Contracts/Accounting/AccountingChargeSucceededIntegrationEvent.cs
namespace SharedMessaging.Contracts.Accounting;

public sealed record AccountingChargeSucceededIntegrationEvent(
    Guid PublicId, int CustomerId, decimal Amount, int? ExternalTransactionId);
