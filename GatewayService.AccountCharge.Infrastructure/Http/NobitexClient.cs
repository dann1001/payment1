// File: GatewayService.AccountCharge.Infrastructure/Http/NobitexClient.cs
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Application.Common;
using GatewayService.AccountCharge.Application.DTOs;
using GatewayService.AccountCharge.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GatewayService.AccountCharge.Infrastructure.Http;

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

    // ---------- Single Source of Truth برای Authorization ----------
    private string GetTokenOrThrow()
    {
        var token = _cfg.Token ?? Environment.GetEnvironmentVariable("NOBITEX_API_TOKEN");
        token = token?.Trim();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Nobitex token is missing. Set Nobitex:Token or NOBITEX_API_TOKEN.");
        return token!;
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url, HttpContent? content = null)
    {
        var req = new HttpRequestMessage(method, url) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Token", GetTokenOrThrow());
        req.Headers.Accept.Clear();
        req.Headers.Accept.ParseAdd("application/json");
        return req;
    }

    public async Task<IReadOnlyList<WalletDto>> GetWalletsAsync(CancellationToken ct)
    {
        using var req = BuildRequest(HttpMethod.Get, "/users/wallets/list");
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

                var currencyLower = (ReadString(w, "currency") ?? string.Empty).Trim().ToLowerInvariant();

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
                    Currency = currencyLower,
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
        var currencyLower = (currency ?? string.Empty).Trim().ToLowerInvariant();
        var networkPayload = string.IsNullOrWhiteSpace(network) ? null : network.Trim();

        var payload = new Dictionary<string, object?>
        {
            ["currency"] = currencyLower,
            ["network"] = networkPayload
        };

        using var req = BuildRequest(HttpMethod.Post, "/users/wallets/generate-address", JsonContent.Create(payload));
        using var res = await _http.SendAsync(req, ct);
        await EnsureSuccessWithRateNotes(res, ct);

        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!IsOk(root))
        {
            var (code, message) = ReadError(root);
            _log.LogWarning("Nobitex generate-address failed. code={Code}, message={Message}, body={Body}", code, message, json);
            throw new InvalidOperationException($"Nobitex generate-address failed: code={code}, message={message}");
        }

        var address = ReadString(root, "address") ?? ReadString(root, "depositAddress");
        var tag = ReadString(root, "memo") ?? ReadString(root, "destinationTag") ?? ReadString(root, "tag");
        if (string.IsNullOrWhiteSpace(address))
        {
            _log.LogError("Nobitex generate-address returned ok but no address. body={Body}", json);
            throw new InvalidOperationException("Nobitex did not return a deposit address.");
        }

        var walletId = ReadIntFlexible(root, "walletId");
        if (walletId == 0) walletId = ReadIntFlexible(root, "wallet");

        return new GeneratedAddressDto
        {
            WalletId = walletId,
            Currency = currencyLower,
            Network = networkPayload,
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

        using var req = BuildRequest(HttpMethod.Get, url);
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
                var symbol = ReadString(d, "currencySymbol");
                var name = ReadString(d, "currency");
                var currency = AssetMapper.NormalizeCurrencyFromProvider(symbol, name);

                var networkRaw = ReadString(d, "network");
                var explorerUrl = ReadString(d, "blockchainUrl");
                var network = AssetMapper.NormalizeNetwork(networkRaw) ?? AssetMapper.InferNetworkFromUrl(explorerUrl);

                var amt = ReadDecimalFlexible(d, "amount");
                var conf = ReadIntFlexible(d, "confirmations");
                var reqConf = ReadIntFlexible(d, "requiredConfirmations");

                var created =
                    ReadDateTimeOffsetFlexible(d, "date") ??
                    ReadDateTimeOffsetFlexibleNested(d, "transaction", "created_at") ??
                    ReadDateTimeOffsetFlexible(d, "created_at") ??
                    DateTimeOffset.UtcNow;

                var confirmed =
                    ReadBoolFlexible(d, "isConfirmed")
                    ?? ReadBoolFlexible(d, "confirmed")
                    ?? ReadBoolFlexible(d, "wasConfirmed")
                    ?? (reqConf > 0 ? conf >= reqConf : conf > 0);

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

    // ---------------- Helpers ----------------
    private static bool IsOk(JsonElement root)
        => string.Equals(ReadString(root, "status"), "ok", StringComparison.OrdinalIgnoreCase);

    private static (string? code, string? message) ReadError(JsonElement root)
        => (ReadString(root, "code"), ReadString(root, "message"));

    private static string? ReadString(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string? ReadNestedString(JsonElement e, string parent, string child)
        => e.TryGetProperty(parent, out var p) && p.ValueKind == JsonValueKind.Object
           && p.TryGetProperty(child, out var c) && c.ValueKind == JsonValueKind.String
                ? c.GetString()
                : null;

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

    private static DateTimeOffset? ReadDateTimeOffsetFlexibleNested(JsonElement e, string parent, string child)
    {
        var s = ReadNestedString(e, parent, child);
        if (s is null) return null;
        return DateTimeOffset.TryParse(s, out var dto) ? dto : (DateTimeOffset?)null;
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
            res.EnsureSuccessStatusCode(); // throws with the 401/4xx body logged above
        }
    }
}
