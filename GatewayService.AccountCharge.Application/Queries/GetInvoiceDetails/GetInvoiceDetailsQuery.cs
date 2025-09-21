using GatewayService.AccountCharge.Application.DTOs;
using MediatR;

namespace GatewayService.AccountCharge.Application.Queries.GetInvoiceDetails;

public sealed record GetInvoiceDetailsQuery(string InvoiceNumber) : IRequest<InvoiceDto>;
