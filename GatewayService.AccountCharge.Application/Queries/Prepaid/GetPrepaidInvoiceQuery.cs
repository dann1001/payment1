using GatewayService.AccountCharge.Application.DTOs;
using MediatR;

namespace GatewayService.AccountCharge.Application.Queries.Prepaid;

public sealed record GetPrepaidInvoiceQuery(Guid Id) : IRequest<PrepaidInvoiceDto>;
