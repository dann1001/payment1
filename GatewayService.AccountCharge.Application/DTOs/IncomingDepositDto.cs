namespace GatewayService.AccountCharge.Application.DTOs;

/// <summary>
/// Transport shape for a raw deposit observed via Nobitex client.
/// </summary>
public sealed class IncomingDepositDto
{
    public required string TxHash { get; init; }
    public required string Address { get; init; }
    public string? Tag { get; init; }                  // memo/destinationTag/tag
    public required string Network { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }     // lowercase
    public required int Confirmations { get; init; }
    public required int RequiredConfirmations { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public int WalletId { get; init; }
    public bool Confirmed { get; init; }

}
