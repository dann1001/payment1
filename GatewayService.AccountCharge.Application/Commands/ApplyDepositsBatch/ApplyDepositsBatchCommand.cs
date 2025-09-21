using GatewayService.AccountCharge.Application.DTOs;
using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.ApplyDeposit;

public sealed record ApplyDepositsBatchCommand(
    IReadOnlyList<IncomingDepositDto> Deposits
) : IRequest<ApplyDepositsBatchResult>;

public sealed record ApplyDepositsBatchResult(
    int Total,
    int Matched,
    int Applied,
    int AlreadyApplied,
    int Rejected
);
