namespace GatewayService.AccountCharge.Domain.Invoices;
/// <summary>
/// Controls how deposits are considered "valid" and how tolerance works.
/// </summary>
public sealed class PaymentMatchingOptions
{
    /// <summary>
    /// Minimal confirmations required to apply a deposit (e.g., 1..network required).
    /// </summary>
    public int MinConfirmations { get; init; } = 1;

    /// <summary>
    /// Absolute tolerance in currency units (e.g., 0.000001 BTC) to account for rounding/netting.
    /// </summary>
    public decimal AbsoluteTolerance { get; init; } = 0m;

    /// <summary>
    /// Percentage tolerance (0..1); e.g., 0.001 = 0.1%.
    /// </summary>
    public decimal PercentageTolerance { get; init; } = 0m;

    /// <summary>
    /// When true, only deposits to known invoice addresses are accepted.
    /// </summary>
    public bool RequireKnownAddress { get; init; } = true;

    /// <summary>
    /// Whether to accept multiple deposits accumulating to expected amount.
    /// </summary>
    public bool AllowMultipleDeposits { get; init; } = true;
}
