using GatewayService.AccountCharge.Domain.Common;

public sealed class ChainAddress : ValueObject
{
    public string Address { get; }
    public string? Tag { get; }
    public string? Network { get; }   // optional

    public ChainAddress(string address, string? network, string? tag = null)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required");

        Address = address.Trim();
        Tag = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim();

        Network = NormalizeNetwork(network);
    }

    private static string? NormalizeNetwork(string? net)
    {
        if (string.IsNullOrWhiteSpace(net)) return null;

        return net.Trim().ToUpperInvariant() switch
        {
            "BEP20" or "BEP-20" or "BSC" => "BSC",
            "TRC20" or "TRON" => "TRC20",
            "ERC20" or "ETHEREUM" => "ERC20",
            _ => net.Trim().ToUpperInvariant()
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Address.ToLowerInvariant();
        yield return Tag?.ToLowerInvariant();
        yield return Network;
    }

    public override string ToString() =>
        Tag is null
            ? $"{Address} ({Network ?? "?"})"
            : $"{Address} ({Network ?? "?"}) [tag:{Tag}]";
}
