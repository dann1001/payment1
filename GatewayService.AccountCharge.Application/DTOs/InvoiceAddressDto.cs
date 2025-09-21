namespace GatewayService.AccountCharge.Application.DTOs;


public sealed class InvoiceAddressDto
{
    public required string Address { get; init; }
    public string? Tag { get; init; }
    public required string Network { get; init; }
    public required int WalletId { get; init; }
    public required string Currency { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
