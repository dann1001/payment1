using GatewayService.AccountCharge.Application.DTOs;
using GatewayService.AccountCharge.Domain.Repositories;
using MediatR;

namespace GatewayService.AccountCharge.Application.Queries.Prepaid;

public sealed class GetPrepaidInvoiceHandler : IRequestHandler<GetPrepaidInvoiceQuery, PrepaidInvoiceDto>
{
    private readonly IPrepaidInvoiceRepository _repo;
    public GetPrepaidInvoiceHandler(IPrepaidInvoiceRepository repo) => _repo = repo;

    public async Task<PrepaidInvoiceDto> Handle(GetPrepaidInvoiceQuery request, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(request.Id, ct) ?? throw new KeyNotFoundException("PrepaidInvoice not found");
        return new PrepaidInvoiceDto
        {
            Id = p.Id,
            CustomerId = p.CustomerId,
            Currency = p.Currency,
            Network = p.Network,
            TxHash = p.TxHash,
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            ExpiresAt = p.ExpiresAt,
            ObservedAmount = p.ObservedAmount,
            ObservedCurrency = p.ObservedCurrency,
            ObservedAddress = p.ObservedAddress,
            ObservedTag = p.ObservedTag,
            ObservedWalletId = p.ObservedWalletId,
            ConfirmationsObserved = p.ConfirmationsObserved,
            RequiredConfirmationsObserved = p.RequiredConfirmationsObserved,
            ConfirmedAt = p.ConfirmedAt,
            LastCheckedAt = p.LastCheckedAt
        };
    }
}
