using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.CreateInvoice;

public sealed record CreateInvoiceCommand(
    string? InvoiceNumber,     // if null, generate
    string Currency,
    decimal Amount,
    string? CustomerId,
    TimeSpan? Ttl
) : IRequest<Guid>; // returns InvoiceId
