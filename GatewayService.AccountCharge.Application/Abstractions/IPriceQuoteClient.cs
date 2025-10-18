// File: D:\GatewayService.AccountCharge\GatewayService.AccountCharge.Application\Abstractions\IPriceQuoteClient.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayService.AccountCharge.Application.Abstractions
{
    /// <summary>
    /// Returns how many USDT per 1 unit of <paramref name="fromCurrency"/> at a given time (UTC).
    /// Example: from=BTC => price like 65000.123456 (USDT).
    /// </summary>
    public interface IPriceQuoteClient
    {
        Task<decimal> GetUsdtQuoteAsync(string fromCurrency, DateTimeOffset atUtc, CancellationToken ct);
    }
}
