using GatewayService.AccountCharge.Domain.Common;
using GatewayService.AccountCharge.Domain.ValueObjects;
using GatewayService.AccountCharge.Domain.Enums;

namespace GatewayService.AccountCharge.Domain.Invoices;
public sealed class InvoiceCreated : DomainEventBase
{
    public Guid InvoiceId { get; }
    public InvoiceCreated(Guid invoiceId) => InvoiceId = invoiceId;
}

public sealed class DepositMatchedToInvoice : DomainEventBase
{
    public Guid InvoiceId { get; }
    public string TxHash { get; }
    public decimal Amount { get; }
    public string Currency { get; }

    public DepositMatchedToInvoice(Guid invoiceId, string txHash, decimal amount, string currency)
    {
        InvoiceId = invoiceId;
        TxHash = txHash;
        Amount = amount;
        Currency = currency;
    }
}

public sealed class InvoiceStatusChanged : DomainEventBase
{
    public Guid InvoiceId { get; }
    public InvoiceStatus From { get; }
    public InvoiceStatus To { get; }

    public InvoiceStatusChanged(Guid invoiceId, InvoiceStatus from, InvoiceStatus to)
    {
        InvoiceId = invoiceId;
        From = from;
        To = to;
    }
}
