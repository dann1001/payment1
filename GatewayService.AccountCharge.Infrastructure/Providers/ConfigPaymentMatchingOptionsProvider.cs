using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Domain.Invoices;
using Microsoft.Extensions.Options;

namespace GatewayService.AccountCharge.Infrastructure.Providers;

public sealed class ConfigPaymentMatchingOptionsProvider : IPaymentMatchingOptionsProvider
{
    private readonly Options.PaymentMatchingOptionsConfig _cfg;

    public ConfigPaymentMatchingOptionsProvider(IOptions<Options.PaymentMatchingOptionsConfig> cfg)
        => _cfg = cfg.Value;

    public PaymentMatchingOptions Get(string currency, string network)
    {
        var key = $"{currency?.ToLowerInvariant()}:{network?.ToUpperInvariant()}";

        // per-network overrides if present
        if (_cfg.Networks is not null && _cfg.Networks.TryGetValue(key, out var per))
        {
            return new PaymentMatchingOptions
            {
                MinConfirmations = per.MinConfirmations ?? _cfg.DefaultMinConfirmations,
                AbsoluteTolerance = per.AbsoluteTolerance ?? _cfg.DefaultAbsoluteTolerance,
                PercentageTolerance = per.PercentageTolerance ?? _cfg.DefaultPercentageTolerance,
                RequireKnownAddress = per.RequireKnownAddress ?? _cfg.DefaultRequireKnownAddress,
                AllowMultipleDeposits = per.AllowMultipleDeposits ?? _cfg.DefaultAllowMultipleDeposits,
            };
        }

        // fallbacks
        return new PaymentMatchingOptions
        {
            MinConfirmations = _cfg.DefaultMinConfirmations,
            AbsoluteTolerance = _cfg.DefaultAbsoluteTolerance,
            PercentageTolerance = _cfg.DefaultPercentageTolerance,
            RequireKnownAddress = _cfg.DefaultRequireKnownAddress,
            AllowMultipleDeposits = _cfg.DefaultAllowMultipleDeposits,
        };
    }
}
