// D:\GatewayService.AccountCharge\GatewayService.AccountCharge.Infrastructure\Options\AccountingOptionsConfig.cs
namespace GatewayService.AccountCharge.Infrastructure.Options;

public sealed class AccountingOptionsConfig
{
    public const string SectionName = "Accounting";
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }          // optional
    public string? UserAgent { get; set; } = "TraderBot/GatewayService.AccountCharge";
}
