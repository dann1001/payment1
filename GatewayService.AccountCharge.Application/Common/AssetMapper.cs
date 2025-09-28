// Application/Common/AssetMapper.cs
using System;

namespace GatewayService.AccountCharge.Application.Common;

public static class AssetMapper
{
    /// <summary>
    /// Normalizes any provider currency label/symbol to our canonical symbol (upper-case).
    /// Examples: "bnb" or "BinanceCoin" -> "BNB", "tether" -> "USDT".
    /// </summary>
    public static string NormalizeCurrency(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Currency code required");

        return code.Trim().ToUpperInvariant() switch
        {
            // Common provider names -> symbols
            "BINANCECOIN" => "BNB",
            "TETHER" => "USDT",
            "BITCOIN" => "BTC",
            "ETHEREUM" => "ETH",

            // already symbols
            "BNB" => "BNB",
            "USDT" => "USDT",
            "BTC" => "BTC",
            "ETH" => "ETH",
            "TRX" => "TRX",

            // default: just upper-case it
            var other => other
        };
    }

    /// <summary>
    /// Prefer symbol when available; otherwise, normalize the full name.
    /// </summary>
    public static string NormalizeCurrencyFromProvider(string? symbol, string? name)
    {
        if (!string.IsNullOrWhiteSpace(symbol))
            return NormalizeCurrency(symbol);
        if (!string.IsNullOrWhiteSpace(name))
            return NormalizeCurrency(name);
        throw new ArgumentException("Provider currency symbol/name is missing.");
    }

    /// <summary>
    /// Canonical network labels aligned with Domain (ChainAddress.NormalizeNetwork):
    /// "BEP20"/"BEP-20"/"BSC" -> "BSC"; "TRC20"/"TRON" -> "TRC20"; "ERC20"/"ETHEREUM"/"ETH" -> "ERC20".
    /// Returns null when unknown/omitted.
    /// </summary>
    public static string? NormalizeNetwork(string? net)
    {
        if (string.IsNullOrWhiteSpace(net)) return null;
        return net.Trim().ToUpperInvariant() switch
        {
            "BEP20" or "BEP-20" or "BSC" => "BSC",
            "TRC20" or "TRON" => "TRC20",
            "ERC20" or "ETHEREUM" or "ETH" => "ERC20",
            var other => other
        };
    }

    /// <summary>
    /// Best-effort inference from blockchain explorer URLs.
    /// </summary>
    public static string? InferNetworkFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var u = url.ToLowerInvariant();
        if (u.Contains("bscscan")) return "BSC";
        if (u.Contains("tronscan")) return "TRC20";
        if (u.Contains("etherscan")) return "ERC20";
        if (u.Contains("polygonscan")) return "POLYGON"; // domain may remap or ignore
        return null;
    }
}
