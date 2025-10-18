// File: Application/Contracts/Deposits/GeneratedAddressResult.cs
namespace GatewayService.AccountCharge.Application.Contracts.Deposits;

public sealed class GeneratedAddressResult
{
    public string Address { get; init; } = default!;
    public string Currency { get; init; } = default!;
    public string? Network { get; init; }
    public int WalletId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
