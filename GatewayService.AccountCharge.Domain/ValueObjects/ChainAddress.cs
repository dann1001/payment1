using GatewayService.AccountCharge.Domain.Common;

namespace GatewayService.AccountCharge.Domain.ValueObjects;

/// <summary>
/// Blockchain deposit address + optional tag/memo + network (e.g., "BTC", "TRX", "ERC20", "BSC").
/// </summary>
public sealed class ChainAddress : ValueObject
{
    public string Address { get; }
    public string? Tag { get; }
    public string Network { get; }

    public ChainAddress(string address, string network, string? tag = null)
    {
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address is required");
        if (string.IsNullOrWhiteSpace(network)) throw new ArgumentException("Network is required");

        Address = address.Trim();
        Network = network.Trim().ToUpperInvariant();
        Tag = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim();
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Address.ToLowerInvariant(); // case-insensitive compare
        yield return Tag?.ToLowerInvariant();
        yield return Network;
    }

    public override string ToString() => Tag is null ? $"{Address} ({Network})" : $"{Address} ({Network}) [tag:{Tag}]";
}
