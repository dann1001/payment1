namespace GatewayService.AccountCharge.Application.DTOs;

public sealed class GeneratedAddressDto
{
    public int WalletId { get; init; }             // 0 if missing
    public required string Currency { get; init; } // lowercase
    public string? Network { get; init; }          // e.g., TRX/BSC/ERC20/BTC
    public required string Address { get; init; }
    public string? Tag { get; init; }              // memo/destinationTag/tag
    public DateTimeOffset CreatedAt { get; init; }
}
