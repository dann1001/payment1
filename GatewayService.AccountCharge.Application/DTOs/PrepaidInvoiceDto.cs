using GatewayService.AccountCharge.Domain.PrepaidInvoices;

namespace GatewayService.AccountCharge.Application.DTOs;

public sealed class PrepaidInvoiceDto
{
    public Guid Id { get; init; }
    public string? CustomerId { get; init; }
    public string Currency { get; init; } = default!;
    public string? Network { get; init; }
    public string TxHash { get; init; } = default!;
    public PrepaidInvoiceStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }

    public decimal? ObservedAmount { get; init; }
    public string? ObservedCurrency { get; init; }
    public string? ObservedAddress { get; init; }
    public string? ObservedTag { get; init; }
    public int? ObservedWalletId { get; init; }
    public int ConfirmationsObserved { get; init; }
    public int RequiredConfirmationsObserved { get; init; }
    public DateTimeOffset? ConfirmedAt { get; init; }
    public DateTimeOffset? LastCheckedAt { get; init; }
}
