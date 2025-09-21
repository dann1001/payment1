namespace GatewayService.AccountCharge.Application.DTOs;

// Simplified view of a deposit-capable wallet (currency + optionally network + id)
public sealed class WalletDto
{
    public int Id { get; init; }
    public required string Currency { get; init; } // lowercase
    public string? Network { get; init; }
    public bool HasDepositAddress { get; init; }
    public string? DepositAddress { get; init; }   // NEW (helpful for fallback)
    public string? DepositTag { get; init; }       // NEW
}
