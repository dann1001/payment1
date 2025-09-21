using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.ApplyDeposit;

public sealed record ApplyDepositCommand(
    string TxHash,
    string Address,
    string Network,
    string? Tag,
    decimal Amount,
    string Currency,
    bool Confirmed,
    int Confirmations,
    int RequiredConfirmations,
    DateTimeOffset CreatedAt
) : IRequest<ApplyDepositResult>;

public sealed record ApplyDepositResult(
    bool Matched,
    bool Applied,           // applied (true) or idempotent/no-op (false)
    string? Reason,         // why not applied (if any)
    Guid? InvoiceId
);
