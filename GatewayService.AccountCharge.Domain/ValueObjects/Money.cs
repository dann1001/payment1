using GatewayService.AccountCharge.Domain.Common;

namespace GatewayService.AccountCharge.Domain.ValueObjects;

/// <summary>
/// Monetary amount with currency code (e.g., "BTC", "ETH", "USDT").
/// Use decimal for safety; keep amounts from Nobitex as string->decimal.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required");
        if (amount < 0) throw new ArgumentException("Amount cannot be negative");
        Currency = currency.Trim().ToUpperInvariant();
        Amount = amount;
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public int CompareTo(Money other)
    {
        EnsureSameCurrency(other);
        return Amount.CompareTo(other.Amount);
    }

    public bool IsZero => Amount == 0m;

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Currency mismatch");
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount} {Currency}";
}
