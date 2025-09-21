using GatewayService.AccountCharge.Application.DTOs;
using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.GenerateAndAttachAddress;

public sealed class GenerateAndAttachAddressToInvoiceCommand : IRequest<GeneratedAddressResult>
{
    public Guid InvoiceId { get; }
    public string Currency { get; }
    public string? Network { get; }

    public GenerateAndAttachAddressToInvoiceCommand(Guid invoiceId, string currency, string? network)
    {
        InvoiceId = invoiceId;
        Currency = currency;
        Network = network;
    }
}
