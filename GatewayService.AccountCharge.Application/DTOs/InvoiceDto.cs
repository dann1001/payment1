using GatewayService.AccountCharge.Application.DTOs;
using GatewayService.AccountCharge.Domain.Enums;

namespace GatewayService.AccountCharge.Application.DTOs;

public sealed class InvoiceDto
{
    public required Guid Id { get; init; }
    public required string InvoiceNumber { get; init; }
    public required string Currency { get; init; }
    public required decimal ExpectedAmount { get; init; }
    public required decimal TotalPaid { get; init; }
    public required InvoiceStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public IReadOnlyCollection<InvoiceAddressDto> Addresses { get; init; } = Array.Empty<InvoiceAddressDto>();
}
