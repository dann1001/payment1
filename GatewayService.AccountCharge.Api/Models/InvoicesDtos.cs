namespace GatewayService.AccountCharge.Api.Models;

// Requests

public sealed class CreateInvoiceRequest
{
    /// <summary>Total expected amount in crypto units (e.g., 0.5)</summary>
    public decimal Amount { get; set; }

    /// <summary>Currency code, e.g., "btc", "eth", "usdt"</summary>
    public string Currency { get; set; } = "btc";

    /// <summary>Optional expiration (UTC). If omitted, backend default applies.</summary>
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>Optional note to store alongside invoice</summary>
    public string? Note { get; set; }
}

public sealed class AttachAddressRequest
{
    /// <summary>Internal wallet id in Nobitex (from /v2/wallets or /users/wallets/list)</summary>
    public int WalletId { get; set; }

    /// <summary>Blockchain deposit address.</summary>
    public string Address { get; set; } = default!;

    /// <summary>Network code (e.g., "BTC", "TRX", "ETH", "BSC").</summary>
    public string Network { get; set; } = "BTC";

    /// <summary>Optional tag/memo (for XRP, XLM, etc.).</summary>
    public string? Tag { get; set; }
}

public sealed class ManualSyncRequest
{
    /// <summary>Override page size for polling recent deposits (default 30)</summary>
    public int? Limit { get; set; }

    /// <summary>Optional lower-bound filter for deposit created_at</summary>
    public DateTime? SinceUtc { get; set; }
}

// Responses

public sealed class CreateInvoiceResponse
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string Status { get; set; } = default!;
}

public sealed class InvoiceDetailsResponse
{
    public string InvoiceNumber { get; set; } = default!;
    public string Status { get; set; } = default!;
    public decimal ExpectedAmount { get; set; }
    public string ExpectedCurrency { get; set; } = default!;
    public decimal TotalPaid { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }

    public List<AddressDto> Addresses { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();

    public sealed class AddressDto
    {
        public int WalletId { get; set; }
        public string Currency { get; set; } = default!;
        public string Address { get; set; } = default!;
        public string Network { get; set; } = default!;
        public string? Tag { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public sealed class PaymentDto
    {
        public string TxHash { get; set; } = default!;
        public string Address { get; set; } = default!;
        public string Network { get; set; } = default!;
        public string? Tag { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = default!;
        public bool Confirmed { get; set; }
        public int Confirmations { get; set; }
        public int RequiredConfirmations { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}

public sealed class ManualSyncResponse
{
    public string InvoiceNumber { get; set; } = default!;
    public int PolledWallets { get; set; }
    public int TotalDepositsSeen { get; set; }
    public int Matched { get; set; }
    public int Applied { get; set; }
    public int AlreadyApplied { get; set; }
    public int Rejected { get; set; }
}
