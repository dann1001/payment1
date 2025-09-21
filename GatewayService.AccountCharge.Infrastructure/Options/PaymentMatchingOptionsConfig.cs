namespace GatewayService.AccountCharge.Infrastructure.Options;

public sealed class PaymentMatchingOptionsConfig
{
    public int DefaultMinConfirmations { get; set; } = 1;
    public decimal DefaultAbsoluteTolerance { get; set; } = 0m;
    public decimal DefaultPercentageTolerance { get; set; } = 0m;
    public bool DefaultRequireKnownAddress { get; set; } = true;
    public bool DefaultAllowMultipleDeposits { get; set; } = true;

    // key: "usdt:TRC20", "btc:BTC", etc.
    public Dictionary<string, PerNetwork>? Networks { get; set; }
}

public sealed class PerNetwork
{
    public int? MinConfirmations { get; set; }
    public decimal? AbsoluteTolerance { get; set; }
    public decimal? PercentageTolerance { get; set; }
    public bool? RequireKnownAddress { get; set; }
    public bool? AllowMultipleDeposits { get; set; }
}
