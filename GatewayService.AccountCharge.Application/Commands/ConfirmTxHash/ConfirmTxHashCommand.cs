// Application/Commands/ConfirmTxHash/ConfirmTxHashCommand.cs
using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.ConfirmTxHash;

public sealed record ConfirmTxHashCommand(Guid InvoiceId, string TxHash)
    : IRequest<ConfirmTxHashResult>;

public sealed record ConfirmTxHashResult(
    Guid? InvoiceId,
    bool FoundOnExchange,   
    bool Matched,         
    bool Applied,          
    string? Reason         
);
