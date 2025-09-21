using GatewayService.AccountCharge.Domain.Common;
using GatewayService.AccountCharge.Domain.Enums;
using GatewayService.AccountCharge.Domain.Exceptions;
using GatewayService.AccountCharge.Domain.ValueObjects;


namespace GatewayService.AccountCharge.Domain.Invoices;
/// <summary>
/// Aggregate Root for crypto payment through exchange deposit.
/// </summary>
public sealed class Invoice : Entity
{
    // Identity & metadata
    public string InvoiceNumber { get; private set; } = default!;
    public string? CustomerId { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Pending;

    // What we expect to receive
    public Money ExpectedAmount { get; private set; } = default!;

    // Window
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    // Addresses reserved for this invoice (1..n)
    private readonly List<InvoiceAddress> _addresses = new();
    public IReadOnlyCollection<InvoiceAddress> Addresses => _addresses;

    // Matched deposits already applied (idempotent via TxHash)
    private readonly List<AppliedDeposit> _appliedDeposits = new();
    public IReadOnlyCollection<AppliedDeposit> AppliedDeposits => _appliedDeposits;

    // Computed totals
    public decimal TotalPaid => _appliedDeposits
        .Where(d => string.Equals(d.Amount.Currency, ExpectedAmount.Currency, StringComparison.OrdinalIgnoreCase))
        .Sum(d => d.Amount.Amount);

    public object Payments { get; set; }

    private Invoice() { } // EF

    private Invoice(string invoiceNumber, Money expectedAmount, string? customerId, DateTime createdAt, DateTime? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber)) throw new ArgumentException("Invoice number is required");
        if (expiresAt.HasValue && expiresAt.Value <= createdAt)
            throw new ArgumentException("ExpiresAt must be greater than CreatedAt");

        InvoiceNumber = invoiceNumber.Trim();
        ExpectedAmount = expectedAmount;
        CustomerId = string.IsNullOrWhiteSpace(customerId) ? null : customerId.Trim();
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;

        Raise(new InvoiceCreated(Id));
    }

    public static Invoice Create(string invoiceNumber, Money expectedAmount, string? customerId, TimeSpan? ttl = null, DateTime? now = null)
    {
        var created = now ?? DateTime.UtcNow;
        DateTime? expires = ttl.HasValue ? created.Add(ttl.Value) : null;
        return new Invoice(invoiceNumber, expectedAmount, customerId, created, expires);
    }

    public void AddAddress(ChainAddress address, WalletRef wallet, DateTimeOffset? now = null)
    {
        if (Status is InvoiceStatus.Paid or InvoiceStatus.Overpaid or InvoiceStatus.Expired or InvoiceStatus.Canceled)
            throw new DomainException("Cannot add address to a closed invoice");

        // Avoid duplicates: same (address + network + tag + walletId)
        if (_addresses.Any(a =>
                string.Equals(a.Address, address.Address, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.Network ?? string.Empty, address.Network ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.Tag ?? string.Empty, address.Tag ?? string.Empty, StringComparison.Ordinal) &&
                a.WalletId == wallet.WalletId))
        {
            return;
        }

        _addresses.Add(new InvoiceAddress(
            walletId: wallet.WalletId,
            currency: wallet.Currency,
            address: address.Address,
            network: address.Network,
            tag: address.Tag,
            createdAt: now ?? DateTimeOffset.UtcNow
        ));
    }

    public bool OwnsAddress(ChainAddress addr) =>
        _addresses.Any(a =>
            string.Equals(a.Address, addr.Address, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.Network ?? string.Empty, addr.Network ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.Tag ?? string.Empty, addr.Tag ?? string.Empty, StringComparison.Ordinal));

    public bool IsExpired(DateTime? now = null)
    {
        if (!ExpiresAt.HasValue) return false;
        return (now ?? DateTime.UtcNow) > ExpiresAt.Value;
    }

    public decimal RemainingToPay() => Math.Max(0m, ExpectedAmount.Amount - TotalPaid);

    /// <summary>
    /// Apply an observed deposit to this invoice if it matches the rules.
    /// Idempotent by TxHash (won't double-apply).
    /// </summary>
    public bool TryApplyDeposit(IncomingDeposit incoming, PaymentMatchingOptions opts, out string reason)
    {
        reason = string.Empty;

        if (IsExpired())
        {
            TransitionTo(InvoiceStatus.Expired);
            reason = "Invoice expired";
            return false;
        }

        // Currency must match invoice currency
        if (!string.Equals(incoming.Amount.Currency, ExpectedAmount.Currency, StringComparison.OrdinalIgnoreCase))
        {
            reason = "Currency mismatch";
            return false;
        }

        // Require known address (strongly recommended)
        if (opts.RequireKnownAddress && !OwnsAddress(incoming.Address))
        {
            reason = "Address is not registered for this invoice";
            return false;
        }

        // Confirmations
        if (incoming.Confirmations < Math.Max(opts.MinConfirmations, incoming.RequiredConfirmations))
        {
            reason = "Not enough confirmations";
            return false;
        }

        // Idempotency by TxHash
        if (_appliedDeposits.Any(d => d.TxHash.Equals(incoming.TxHash)))
        {
            reason = "Already applied";
            return true; // idempotent no-op
        }

        // If multiple deposits are not allowed and we already have some, reject
        if (!opts.AllowMultipleDeposits && _appliedDeposits.Any())
        {
            reason = "Multiple deposits not allowed";
            return false;
        }

        // Apply
        var applied = new AppliedDeposit(
            invoiceId: Id,
            txHash: incoming.TxHash,
            address: incoming.Address,
            amount: incoming.Amount,
            wasConfirmed: incoming.Confirmed,
            confirmations: incoming.Confirmations,
            requiredConfirmations: incoming.RequiredConfirmations,
            observedAt: incoming.CreatedAt
        );

        _appliedDeposits.Add(applied);
        Raise(new DepositMatchedToInvoice(Id, incoming.TxHash.Value, incoming.Amount.Amount, incoming.Amount.Currency));

        // Re-evaluate status
        UpdateStatusWithTolerance(opts);

        return true;
    }

    private void UpdateStatusWithTolerance(PaymentMatchingOptions opts)
    {
        var prev = Status;

        var paid = TotalPaid;
        var expected = ExpectedAmount.Amount;

        var absTol = opts.AbsoluteTolerance;
        var pctTol = opts.PercentageTolerance > 0 ? expected * opts.PercentageTolerance : 0m;
        var tol = Math.Max(absTol, pctTol);

        if (paid >= expected - tol && paid <= expected + tol)
        {
            TransitionTo(InvoiceStatus.Paid);
        }
        else if (paid < expected - tol && paid > 0m)
        {
            TransitionTo(InvoiceStatus.PartiallyPaid);
        }
        else if (paid > expected + tol)
        {
            TransitionTo(InvoiceStatus.Overpaid);
        }
        else if (paid == 0m)
        {
            TransitionTo(InvoiceStatus.Pending);
        }

        if (Status != prev)
        {
            Raise(new InvoiceStatusChanged(Id, prev, Status));
        }
    }

    public void MarkExpired(DateTime? now = null)
    {
        if (Status is InvoiceStatus.Paid or InvoiceStatus.Overpaid or InvoiceStatus.Canceled) return;
        if (!IsExpired(now)) return;
        TransitionTo(InvoiceStatus.Expired);
        Raise(new InvoiceStatusChanged(Id, InvoiceStatus.Pending, InvoiceStatus.Expired));
    }

    public void Cancel()
    {
        if (Status is InvoiceStatus.Paid or InvoiceStatus.Overpaid or InvoiceStatus.Expired)
            throw new DomainException("Cannot cancel a closed invoice");
        TransitionTo(InvoiceStatus.Canceled);
        Raise(new InvoiceStatusChanged(Id, Status, InvoiceStatus.Canceled));
    }

    private void TransitionTo(InvoiceStatus newStatus)
    {
        Status = newStatus;
    }
}

