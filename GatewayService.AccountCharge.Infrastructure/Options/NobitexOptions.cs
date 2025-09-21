namespace GatewayService.AccountCharge.Infrastructure.Options;

public sealed class NobitexOptionsConfig
{
    public const string SectionName = "Nobitex";
    public string? BaseUrl { get; set; }
    public string? UserAgent { get; set; }
    public string? Token { get; set; }
}
