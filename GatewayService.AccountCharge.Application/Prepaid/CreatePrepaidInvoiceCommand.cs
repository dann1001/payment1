using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.Prepaid;

public sealed record CreatePrepaidInvoiceCommand(
    string Currency,
    string? Network,
    string TxHash,
    string? CustomerId,
    TimeSpan? Ttl
) : IRequest<Guid>;
