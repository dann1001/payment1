using System;
using System.Linq;
using GatewayService.AccountCharge.Application.DTOs;
using GatewayService.AccountCharge.Domain.Repositories;
using MediatR;

namespace GatewayService.AccountCharge.Application.Queries.GetInvoiceDetails;

public sealed class GetInvoiceDetailsHandler : IRequestHandler<GetInvoiceDetailsQuery, InvoiceDto>
{
    private readonly IInvoiceRepository _repo;

    public GetInvoiceDetailsHandler(IInvoiceRepository repo) => _repo = repo;

    public async Task<InvoiceDto> Handle(GetInvoiceDetailsQuery request, CancellationToken ct)
    {
        var invoice = await _repo.GetByNumberAsync(request.InvoiceNumber.Trim(), ct)
            ?? throw new KeyNotFoundException("Invoice not found");

        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Currency = invoice.ExpectedAmount.Currency,
            ExpectedAmount = invoice.ExpectedAmount.Amount,
            TotalPaid = invoice.TotalPaid,
            Status = invoice.Status,

            // invoice.CreatedAt/ExpiresAt در دامین هنوز DateTime هستند → به Offset تبدیل‌شان می‌کنیم
            CreatedAt = new DateTimeOffset(invoice.CreatedAt, TimeSpan.Zero),
            ExpiresAt = invoice.ExpiresAt.HasValue
                ? new DateTimeOffset(invoice.ExpiresAt.Value, TimeSpan.Zero)
                : (DateTimeOffset?)null,

            Addresses = invoice.Addresses.Select(a => new InvoiceAddressDto
            {
                Address = a.Address,        // string
                Tag = a.Tag,            // string?
                Network = a.Network,        // string?
                WalletId = a.WalletId,      // int
                Currency = a.Currency,      // string
                CreatedAt = a.CreatedAt     // DateTimeOffset
            }).ToArray()
        };
    }
}
