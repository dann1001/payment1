using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.AttachAddress;

public sealed record AttachAddressToInvoiceCommand(
    Guid InvoiceId,
    string Address,
    string Network,
    string? Tag,
    int WalletId,
    string Currency
) : IRequest<bool>; // true if attached or already present
