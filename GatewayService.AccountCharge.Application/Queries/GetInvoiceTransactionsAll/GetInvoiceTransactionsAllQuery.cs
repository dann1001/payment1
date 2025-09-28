using System;
using System.Collections.Generic;
using GatewayService.AccountCharge.Application.DTOs;
using MediatR;

namespace GatewayService.AccountCharge.Application.Queries.GetInvoiceTransactionsAll;

public sealed record GetInvoiceTransactionsAllQuery(Guid InvoiceId)
    : IRequest<IReadOnlyList<IncomingDepositDto>>;
