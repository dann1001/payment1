// File: D:\GatewayService.AccountCharge\GatewayService.AccountCharge.Infrastructure\Http\PriceQuoteClient.cs
using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace GatewayService.AccountCharge.Infrastructure.Http
{
    /// <summary>
    /// Quotes how many USDT per 1 unit of a given currency by:
    /// 1) finding the pair id in /api/pairslist (base=FROM, quote=USDT)
    /// 2) fetching live price via /api/exchangeId?id={pairId}
    /// </summary>
    public sealed class PriceQuoteClient : IPriceQuoteClient
    {
        private readonly HttpClient _http;
        private readonly PriceQuoteOptionsConfig _cfg;

        public PriceQuoteClient(HttpClient http, IOptions<PriceQuoteOptionsConfig> cfg)
        {
            _http = http;
            _cfg = cfg.Value;
        }

        public async Task<decimal> GetUsdtQuoteAsync(string fromCurrency, DateTimeOffset atUtc, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(fromCurrency))
                throw new ArgumentException("fromCurrency is required", nameof(fromCurrency));

            var from = fromCurrency.Trim().ToUpperInvariant();
            var quote = "USDT";

            // 1) Load pairs and find id for (base=from, quote=USDT)
            var pairsPath = NormalizeRelPath(_cfg.PairsListPath, "api/pairslist");
            using var pairsReq = new HttpRequestMessage(HttpMethod.Get, pairsPath);
            using var pairsRes = await _http.SendAsync(pairsReq, ct);
            pairsRes.EnsureSuccessStatusCode();

            var pairsJson = await pairsRes.Content.ReadAsStringAsync(ct);
            using var pairsDoc = JsonDocument.Parse(pairsJson);
            var pairsRoot = pairsDoc.RootElement;

            var pairId = FindPairId(pairsRoot, from, quote);
            if (pairId == 0)
                throw new InvalidOperationException($"Pair not found for {from}/{quote}. Body={pairsJson}");

            // 2) Live price by pair id
            var exchPath = NormalizeRelPath(_cfg.ExchangeIdPath, "api/exchangeId");
            var qk = string.IsNullOrWhiteSpace(_cfg.ExchangeIdQueryKey) ? "id" : _cfg.ExchangeIdQueryKey.Trim();
            var url = $"{exchPath}?{qk}={pairId}";

            using var pxReq = new HttpRequestMessage(HttpMethod.Get, url);
            using var pxRes = await _http.SendAsync(pxReq, ct);
            pxRes.EnsureSuccessStatusCode();

            var pxJson = await pxRes.Content.ReadAsStringAsync(ct);
            using var pxDoc = JsonDocument.Parse(pxJson);
            var pxRoot = pxDoc.RootElement;

            var price = ReadDecimalFlexible(pxRoot, "price");
            if (price <= 0m) price = ReadDecimalFlexible(pxRoot, "lastPrice");
            if (price <= 0m) price = ReadDecimalFlexible(pxRoot, "value");
            if (price <= 0m) price = ReadDecimalFlexible(pxRoot, "rate");

            if (price <= 0m)
                throw new InvalidOperationException($"ExchangeId returned invalid price for pairId={pairId}. Body={pxJson}");

            return price;
        }

        private static string NormalizeRelPath(string? p, string fallback)
        {
            var rel = string.IsNullOrWhiteSpace(p) ? fallback : p!.Trim();
            return rel.TrimStart('/');
        }

        private static int FindPairId(JsonElement root, string baseSym, string quoteSym)
        {
            // Accept { "pairs":[...] }, { "data":[...] } or top-level array
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("pairs", out var pairs) && pairs.ValueKind == JsonValueKind.Array)
                    return FindInArray(pairs, baseSym, quoteSym);
                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                    return FindInArray(data, baseSym, quoteSym);
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                return FindInArray(root, baseSym, quoteSym);
            }

            return 0;
        }

        private static int FindInArray(JsonElement arr, string baseSym, string quoteSym)
        {
            foreach (var e in arr.EnumerateArray())
            {
                var b = ReadSymbolFlexible(e, "base") ?? ReadSymbolFlexible(e, "from");
                var q = ReadSymbolFlexible(e, "quote") ?? ReadSymbolFlexible(e, "to");
                if (b is null || q is null) continue;

                if (string.Equals(b, baseSym, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(q, quoteSym, StringComparison.OrdinalIgnoreCase))
                {
                    var id = ReadIntFlexible(e, "id");
                    if (id != 0) return id;
                }
            }
            return 0;
        }

        private static string? ReadSymbolFlexible(JsonElement e, string role /*base|quote|from|to*/)
        {
            // Try multiple common shapes: base, baseSymbol, baseCurrency, symbolBase
            string? Try(string name)
                => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
                    ? v.GetString()
                    : null;

            return (role switch
            {
                "base" or "from" => Try("base") ?? Try("baseSymbol") ?? Try("baseCurrency") ?? Try("from") ?? Try("fromSymbol"),
                "quote" or "to" => Try("quote") ?? Try("quoteSymbol") ?? Try("quoteCurrency") ?? Try("to") ?? Try("toSymbol"),
                _ => null
            })?.Trim().ToUpperInvariant();
        }

        private static int ReadIntFlexible(JsonElement e, string name)
        {
            if (!e.TryGetProperty(name, out var v)) return 0;
            return v.ValueKind switch
            {
                JsonValueKind.Number when v.TryGetInt32(out var i) => i,
                JsonValueKind.String when int.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s) => s,
                _ => 0
            };
        }

        private static decimal ReadDecimalFlexible(JsonElement e, string name)
        {
            if (!e.TryGetProperty(name, out var v)) return 0m;
            return v.ValueKind switch
            {
                JsonValueKind.Number when v.TryGetDecimal(out var d) => d,
                JsonValueKind.String when decimal.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s) => s,
                _ => 0m
            };
        }
    }
}
