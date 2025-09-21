using GatewayService.AccountCharge.Domain.Common;

namespace GatewayService.AccountCharge.Domain.ValueObjects;

public sealed class TransactionHash : ValueObject
{
    public string Value { get; }

    public TransactionHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Tx hash is required");
        Value = value.Trim();
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;
}
