// File: D:\GatewayService.AccountCharge\GatewayService.AccountCharge.Infrastructure\Options\PriceQuoteOptionsConfig.cs
using System;

namespace GatewayService.AccountCharge.Infrastructure.Options
{
    /// <summary>
    /// Config for colleague price service.
    /// </summary>
    public sealed class PriceQuoteOptionsConfig
    {
        public const string SectionName = "PriceQuote";

        /// <summary>Base URL of the colleague price service, e.g. "http://172.31.18.2:8082/".</summary>
        public string? BaseUrl { get; set; }

        /// <summary>Relative path for listing pairs (returns list with id/base/quote).</summary>
        public string PairsListPath { get; set; } = "api/pairslist";

        /// <summary>Relative path for getting live price by pair id.</summary>
        public string ExchangeIdPath { get; set; } = "api/exchangeId";

        /// <summary>Query key for exchangeId endpoint.</summary>
        public string ExchangeIdQueryKey { get; set; } = "id";

        /// <summary>Optional User-Agent header.</summary>
        public string? UserAgent { get; set; } = "TraderBot/GatewayService.AccountCharge";
    }
}
