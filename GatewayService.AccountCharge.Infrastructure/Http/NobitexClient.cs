using System.Net.Http.Json;
using System.Text.Json;
using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Application.DTOs;
using GatewayService.AccountCharge.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GatewayService.AccountCharge.Infrastructure.Http;

/// <summary>
/// Production-grade Nobitex client: status check, tolerant JSON parsing, and network normalization.
/// </summary>
public sealed class NobitexClient : INobitexClient
{
    private readonly HttpClient _http;
    private readonly ILogger<NobitexClient> _log;
    private readonly NobitexOptionsConfig _cfg;

    public NobitexClient(HttpClient http, IOptions<NobitexOptionsConfig> cfg, ILogger<NobitexClient> log)
    {
        _http = http;
        _cfg = cfg.Value;
        _log = log;
    }

    public async Task<IReadOnlyList<WalletDto>> GetWalletsAsync(CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/users/wallets/list");
        using var res = await _http.SendAsync(req, ct);
        await EnsureSuccessWithRateNotes(res, ct);

        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!IsOk(root))
        {
            var (code, message) = ReadError(root);
            _log.LogWarning("Nobitex wallets/list failed. code={Code}, message={Message}, body={Body}", code, message, json);
            throw new InvalidOperationException($"Nobitex wallets/list failed: code={code}, message={message}");
        }

        var list = new List<WalletDto>();
        if (root.TryGetProperty("wallets", out var wallets) && wallets.ValueKind == JsonValueKind.Array)
        {
            foreach (var w in wallets.EnumerateArray())
            {
                var id = ReadIntFlexible(w, "id");
                if (id == 0) id = ReadIntFlexible(w, "wallet");

                var currency = (ReadString(w, "currency") ?? "").ToLowerInvariant();

                string? network = null;
                string? depAddr = null;
                string? depTag = null;

                if (w.TryGetProperty("depositInfo", out var dep) && dep.ValueKind == JsonValueKind.Object)
                {
                    depAddr = ReadString(dep, "address");
                    depTag = ReadString(dep, "tag") ?? ReadString(dep, "memo") ?? ReadString(dep, "destinationTag");
                    network = ReadString(dep, "network");
                }

                list.Add(new WalletDto
                {
                    Id = id,
                    Currency = currency,
                    Network = network,
                    HasDepositAddress = !string.IsNullOrWhiteSpace(depAddr),
                    DepositAddress = string.IsNullOrWhiteSpace(depAddr) ? null : depAddr!.Trim(),
                    DepositTag = string.IsNullOrWhiteSpace(depTag) ? null : depTag!.Trim()
                });
            }
        }

        return list;
    }

    public async Task<GeneratedAddressDto> GenerateAddressAsync(string currency, string? network, CancellationToken ct)
    {
        var normalizedCurrency = (currency ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedNetwork = NormalizeNetwork(normalizedCurrency, network);

        var payload = new Dictionary<string, object?>
        {
            ["currency"] = normalizedCurrency,
            ["network"] = string.IsNullOrWhiteSpace(normalizedNetwork) ? null : normalizedNetwork
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "/users/wallets/generate-address")
        {
            Content = JsonContent.Create(payload)
        };

        using var res = await _http.SendAsync(req, ct);
        await EnsureSuccessWithRateNotes(res, ct);

        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // status check (even when HTTP 200)
        if (!IsOk(root))
        {
            var (code, message) = ReadError(root);
            _log.LogWarning("Nobitex generate-address failed. code={Code}, message={Message}, body={Body}", code, message, json);
            throw new InvalidOperationException($"Nobitex generate-address failed: code={code}, message={message}");
        }

        // address + tag (aliases)
        var address = ReadString(root, "address") ?? ReadString(root, "depositAddress");
        var tag = ReadString(root, "memo") ?? ReadString(root, "destinationTag") ?? ReadString(root, "tag");

        if (string.IsNullOrWhiteSpace(address))
        {
            _log.LogError("Nobitex generate-address returned ok but no address. body={Body}", json);
            throw new InvalidOperationException("Nobitex did not return a deposit address.");
        }

        // wallet id (optional)
        var walletId = ReadIntFlexible(root, "walletId");
        if (walletId == 0) walletId = ReadIntFlexible(root, "wallet");

        return new GeneratedAddressDto
        {
            WalletId = walletId,
            Currency = normalizedCurrency,
            Network = normalizedNetwork,
            Address = address.Trim(),
            Tag = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<IReadOnlyList<IncomingDepositDto>> GetRecentDepositsAsync(int walletId, int limit, DateTimeOffset? since, CancellationToken ct)
    {
        var url = $"/users/wallets/deposits/list?wallet={walletId}&limit={limit}";
        if (since.HasValue)
            url += $"&startDate={Uri.EscapeDataString(since.Value.UtcDateTime.ToString("o"))}";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var res = await _http.SendAsync(req, ct);
        await EnsureSuccessWithRateNotes(res, ct);

        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!IsOk(root))
        {
            var (code, message) = ReadError(root);
            _log.LogWarning("Nobitex deposits/list failed. code={Code}, message={Message}, body={Body}", code, message, json);
            throw new InvalidOperationException($"Nobitex deposits/list failed: code={code}, message={message}");
        }

        var list = new List<IncomingDepositDto>();
        if (root.TryGetProperty("deposits", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var d in arr.EnumerateArray())
            {
                var txHash = ReadString(d, "txHash") ?? ReadString(d, "txid") ?? string.Empty;
                var address = ReadString(d, "address") ?? string.Empty;
                var memo = ReadString(d, "memo") ?? ReadString(d, "destinationTag") ?? ReadString(d, "tag");
                var currency = (ReadString(d, "currency") ?? string.Empty).ToLowerInvariant();
                var network = ReadString(d, "network") ?? string.Empty;
                var amt = ReadDecimalFlexible(d, "amount");
                var conf = ReadIntFlexible(d, "confirmations");
                var reqConf = ReadIntFlexible(d, "requiredConfirmations");
                var created = ReadDateTimeOffsetFlexible(d, "createdAt") ?? DateTimeOffset.UtcNow;

                // prefer explicit booleans if present; else derive
                var wasConfirmedFlag =
                       ReadBoolFlexible(d, "wasConfirmed")
                    ?? ReadBoolFlexible(d, "confirmed")
                    ?? (bool?)null;

                var confirmed = wasConfirmedFlag ?? (reqConf > 0 ? conf >= reqConf : conf > 0);

                list.Add(new IncomingDepositDto
                {
                    WalletId = walletId,
                    Address = address,
                    Tag = string.IsNullOrWhiteSpace(memo) ? null : memo,
                    TxHash = txHash,
                    Amount = amt,
                    Currency = currency,
                    Network = network,
                    Confirmations = conf,
                    RequiredConfirmations = reqConf,
                    CreatedAt = created,
                    Confirmed = confirmed
                });
            }
        }

        return list;
    }

    // -----------------------
    // Helpers
    // -----------------------

    private static bool IsOk(JsonElement root)
    {
        var status = ReadString(root, "status");
        return string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase);
    }

    private static (string? code, string? message) ReadError(JsonElement root)
    {
        var code = ReadString(root, "code");
        var msg = ReadString(root, "message");
        return (code, msg);
    }

    private static string? ReadString(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int ReadIntFlexible(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var v)) return 0;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i;
        if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var si)) return si;
        return 0;
    }

    private static decimal ReadDecimalFlexible(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var v)) return 0m;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d)) return d;
        if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(), out var sd)) return sd;
        return 0m;
    }

    private static DateTimeOffset? ReadDateTimeOffsetFlexible(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var v)) return null;
        if (v.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(v.GetString(), out var dto)) return dto;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var unixMs)) return DateTimeOffset.FromUnixTimeMilliseconds(unixMs);
        return null;
    }

    private static bool? ReadBoolFlexible(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(v.GetString(), out var b) ? b : null,
            JsonValueKind.Number => v.TryGetInt32(out var i) ? i != 0 : (bool?)null,
            _ => null
        };
    }

    private static string? NormalizeNetwork(string? currencyLower, string? network)
    {
        if (string.IsNullOrWhiteSpace(network)) return null;
        var n = network.Trim();

        // Example normalization: USDT TRC20 -> TRX, ETH -> ERC20, BEP20 -> BSC
        if (!string.IsNullOrEmpty(currencyLower) && currencyLower.Equals("usdt", StringComparison.Ordinal))
        {
            if (n.Equals("TRC20", StringComparison.OrdinalIgnoreCase)) return "TRX";
            if (n.Equals("ETH", StringComparison.OrdinalIgnoreCase)) return "ERC20";
            if (n.Equals("BEP20", StringComparison.OrdinalIgnoreCase)) return "BSC";
        }
        return n.ToUpperInvariant();
    }

    private async Task EnsureSuccessWithRateNotes(HttpResponseMessage res, CancellationToken ct)
    {
        if ((int)res.StatusCode == 429)
        {
            var retryAfter = res.Headers.RetryAfter?.Delta ?? TimeSpan.Zero;
            _log.LogWarning("Nobitex 429 TooManyRequests. Retry-After: {RetryAfter}", retryAfter);
        }

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            _log.LogError("Nobitex HTTP error {Status}: {Body}", (int)res.StatusCode, body);
            res.EnsureSuccessStatusCode();
        }
    }
}
