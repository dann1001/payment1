using System.Collections.Generic;
using MediatR;
using GatewayService.AccountCharge.Application.DTOs;

namespace GatewayService.AccountCharge.Application.Queries.GetInvoiceTransactions;

public sealed record GetInvoiceTransactionsQuery(
    Guid InvoiceId,
    int? Limit,
    DateTimeOffset? SinceUtc,
    bool OnlyInvoiceAddresses = true
) : IRequest<IReadOnlyList<IncomingDepositDto>>;
