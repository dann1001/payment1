using GatewayService.AccountCharge.Domain.Common;
using GatewayService.AccountCharge.Domain.ValueObjects;

namespace GatewayService.AccountCharge.Domain.Invoices;
/// <summary>
/// Deposit that has been matched and applied to an invoice (persisted).
/// </summary>
public sealed class AppliedDeposit : Entity
{
    public Guid Id { get; private set; }                // GUID PK
    public Guid InvoiceId { get; private set; }
    public TransactionHash TxHash { get; private set; } = default!;
    public ChainAddress Address { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;
    public DateTimeOffset ObservedAt { get; private set; }
    public bool WasConfirmed { get; private set; }
    public int Confirmations { get; private set; }
    public int RequiredConfirmations { get; private set; }

    private AppliedDeposit() { } // EF

    internal AppliedDeposit(

        Guid invoiceId,
        TransactionHash txHash,
        ChainAddress address,
        Money amount,
        bool wasConfirmed,
        int confirmations,
        int requiredConfirmations,
        DateTimeOffset observedAt)
    {
        Id = Guid.NewGuid();            // <<<< generate here
        InvoiceId = invoiceId;
        TxHash = txHash;
        Address = address;
        Amount = amount;
        WasConfirmed = wasConfirmed;
        Confirmations = confirmations;
        RequiredConfirmations = requiredConfirmations;
        ObservedAt = observedAt;
    }
}
