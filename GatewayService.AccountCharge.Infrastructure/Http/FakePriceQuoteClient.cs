using System;
using System.Threading;
using System.Threading.Tasks;
using GatewayService.AccountCharge.Application.Abstractions;

namespace GatewayService.AccountCharge.Infrastructure.Http
{
    /// <summary>
    /// Fake implementation that returns a static or random quote for test environments.
    /// </summary>
    public sealed class FakePriceQuoteClient : IPriceQuoteClient
    {
        public Task<decimal> GetUsdtQuoteAsync(string fromCurrency, DateTimeOffset atUtc, CancellationToken ct)
        {
            // Simulate a fixed rate for test: e.g., 1 BNB = 600 USDT, 1 ETH = 3000 USDT
            var cur = fromCurrency.Trim().ToUpperInvariant();
            decimal rate = cur switch
            {
                "BNB" => 600.00m,
                "ETH" => 3000.00m,
                "TRX" => 0.10m,
                _ => 1.00m
            };

            return Task.FromResult(rate);
        }
    }
}
