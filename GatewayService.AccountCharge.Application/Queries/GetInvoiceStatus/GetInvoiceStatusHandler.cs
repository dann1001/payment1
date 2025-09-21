using System;
using System.Linq;
using GatewayService.AccountCharge.Application.DTOs;
using GatewayService.AccountCharge.Domain.Repositories;
using MediatR;

namespace GatewayService.AccountCharge.Application.Queries.GetInvoiceStatus;

public sealed class GetInvoiceStatusHandler : IRequestHandler<GetInvoiceStatusQuery, InvoiceDto>
{
    private readonly IInvoiceRepository _repo;

    public GetInvoiceStatusHandler(IInvoiceRepository repo) => _repo = repo;

    public async Task<InvoiceDto> Handle(GetInvoiceStatusQuery request, CancellationToken ct)
    {
        var invoice = await _repo.GetByIdAsync(request.InvoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found");

        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Currency = invoice.ExpectedAmount.Currency,
            ExpectedAmount = invoice.ExpectedAmount.Amount,
            TotalPaid = invoice.TotalPaid,
            Status = invoice.Status,

            // Domain: DateTime → DTO: DateTimeOffset
            CreatedAt = new DateTimeOffset(invoice.CreatedAt, TimeSpan.Zero),
            ExpiresAt = invoice.ExpiresAt.HasValue
                ? new DateTimeOffset(invoice.ExpiresAt.Value, TimeSpan.Zero)
                : (DateTimeOffset?)null,

            // InvoiceAddress اسکالرها
            Addresses = invoice.Addresses.Select(a => new InvoiceAddressDto
            {
                Address = a.Address,
                Tag = a.Tag,
                Network = a.Network,
                WalletId = a.WalletId,
                Currency = a.Currency,
                CreatedAt = a.CreatedAt // DateTimeOffset
            }).ToArray()
        };
    }
}
