using GatewayService.AccountCharge.Domain.Common;
using GatewayService.AccountCharge.Domain.PrepaidInvoices;

namespace GatewayService.AccountCharge.Domain.PrepaidInvoices;

public sealed class PrepaidInvoice : Entity
{
    public string? CustomerId { get; private set; }

    public string Currency { get; private set; } = default!;
    public string? Network { get; private set; }        // optional
    public string TxHash { get; private set; } = default!;

    public PrepaidInvoiceStatus Status { get; private set; } = PrepaidInvoiceStatus.AwaitingConfirmations;

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    // Observation snapshot (آخرین دیده‌شده از اکسچنج)
    public decimal? ObservedAmount { get; private set; }
    public string? ObservedCurrency { get; private set; }
    public string? ObservedAddress { get; private set; }
    public string? ObservedTag { get; private set; }
    public int? ObservedWalletId { get; private set; }
    public int ConfirmationsObserved { get; private set; }
    public int RequiredConfirmationsObserved { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? LastCheckedAt { get; private set; }

    private PrepaidInvoice() { } // EF

    private PrepaidInvoice(string currency, string? network, string txHash, string? customerId, DateTimeOffset createdAt, DateTimeOffset? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required");
        if (string.IsNullOrWhiteSpace(txHash)) throw new ArgumentException("TxHash is required");
        if (expiresAt.HasValue && expiresAt <= createdAt) throw new ArgumentException("ExpiresAt must be after CreatedAt");

        Currency = currency.Trim().ToUpperInvariant();
        Network = string.IsNullOrWhiteSpace(network) ? null : network.Trim().ToUpperInvariant();
        TxHash = txHash.Trim();
        CustomerId = string.IsNullOrWhiteSpace(customerId) ? null : customerId.Trim();

        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public static PrepaidInvoice Create(string currency, string? network, string txHash, string? customerId, TimeSpan? ttl = null, DateTimeOffset? now = null)
    {
        var created = now ?? DateTimeOffset.UtcNow;
        var expires = ttl.HasValue ? created.Add(ttl.Value) : (DateTimeOffset?)null;
        return new PrepaidInvoice(currency, network, txHash, customerId, created, expires);
    }

    public bool IsExpired(DateTimeOffset? now = null)
        => ExpiresAt.HasValue && (now ?? DateTimeOffset.UtcNow) > ExpiresAt.Value;

    public void MarkRejectedWrongAddress(string? addr, string? tag, int? walletId, DateTimeOffset? observedAt)
    {
        Status = PrepaidInvoiceStatus.RejectedWrongAddress;
        ObservedAddress = addr;
        ObservedTag = tag;
        ObservedWalletId = walletId;
        LastCheckedAt = observedAt ?? DateTimeOffset.UtcNow;
    }

    public void MarkRejectedCurrencyMismatch(string observedCurrency, DateTimeOffset? observedAt)
    {
        Status = PrepaidInvoiceStatus.RejectedCurrencyMismatch;
        ObservedCurrency = observedCurrency;
        LastCheckedAt = observedAt ?? DateTimeOffset.UtcNow;
    }

    public void MarkAwaiting(int confirmations, int required, decimal amount, string currency, string? addr, string? tag, int? walletId, DateTimeOffset createdAt)
    {
        Status = PrepaidInvoiceStatus.AwaitingConfirmations;
        ConfirmationsObserved = confirmations;
        RequiredConfirmationsObserved = required;
        ObservedAmount = amount;
        ObservedCurrency = currency;
        ObservedAddress = addr;
        ObservedTag = tag;
        ObservedWalletId = walletId;
        LastCheckedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPaid(decimal amount, string currency, string? addr, string? tag, int? walletId, int confirmations, int required, DateTimeOffset occurredAt)
    {
        Status = PrepaidInvoiceStatus.Paid;
        ObservedAmount = amount;
        ObservedCurrency = currency;
        ObservedAddress = addr;
        ObservedTag = tag;
        ObservedWalletId = walletId;
        ConfirmationsObserved = confirmations;
        RequiredConfirmationsObserved = required;
        ConfirmedAt = occurredAt;
        LastCheckedAt = DateTimeOffset.UtcNow;
    }

    public void MarkExpired()
    {
        if (Status == PrepaidInvoiceStatus.Paid) return;
        Status = PrepaidInvoiceStatus.Expired;
    }

    public void MarkAccountingSyncFailed()
    {
        if (Status == PrepaidInvoiceStatus.Paid)
            Status = PrepaidInvoiceStatus.AccountingSyncFailed;
    }
}
