using GatewayService.AccountCharge.Domain.Common;

namespace GatewayService.AccountCharge.Domain.ValueObjects;

/// <summary>
/// Reference to the exchange wallet in Nobitex (wallet id + currency code).
/// </summary>
public sealed class WalletRef : ValueObject
{
    public int WalletId { get; }
    public string Currency { get; }

    public WalletRef(int walletId, string currency)
    {
        if (walletId <= 0) throw new ArgumentException("Invalid wallet id");
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required");

        WalletId = walletId;
        Currency = currency.Trim().ToUpperInvariant();
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return WalletId;
        yield return Currency;
    }

    public override string ToString() => $"{Currency}#{WalletId}";
}
