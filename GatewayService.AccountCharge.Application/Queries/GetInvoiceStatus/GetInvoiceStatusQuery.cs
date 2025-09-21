using GatewayService.AccountCharge.Application.DTOs;
using MediatR;

namespace GatewayService.AccountCharge.Application.Queries.GetInvoiceStatus;

public sealed record GetInvoiceStatusQuery(Guid InvoiceId) : IRequest<InvoiceDto>;
