using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.Prepaid;

public sealed record SyncPrepaidInvoiceCommand(Guid Id) : IRequest<bool>; // true if state changed
