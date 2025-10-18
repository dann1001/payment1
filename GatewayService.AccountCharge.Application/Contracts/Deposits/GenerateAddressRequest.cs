// File: Application/Contracts/Deposits/GenerateAddressRequest.cs
namespace GatewayService.AccountCharge.Application.Contracts.Deposits;

public sealed class GenerateAddressRequest
{
    public string Currency { get; set; } = default!;
    public string? Network { get; set; }
}
