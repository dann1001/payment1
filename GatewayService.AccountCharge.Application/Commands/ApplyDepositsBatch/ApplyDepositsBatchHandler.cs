using GatewayService.AccountCharge.Application.Commands.ApplyDeposit;
using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.ApplyDeposit;

public sealed class ApplyDepositsBatchHandler : IRequestHandler<ApplyDepositsBatchCommand, ApplyDepositsBatchResult>
{
    private readonly IMediator _mediator;

    public ApplyDepositsBatchHandler(IMediator mediator) => _mediator = mediator;

    public async Task<ApplyDepositsBatchResult> Handle(ApplyDepositsBatchCommand request, CancellationToken ct)
    {
        int matched = 0, applied = 0, already = 0, rejected = 0;

        foreach (var d in request.Deposits)
        {
            var res = await _mediator.Send(new ApplyDepositCommand(
                d.TxHash, d.Address, d.Network, d.Tag, d.Amount, d.Currency, d.Confirmed,
                d.Confirmations, d.RequiredConfirmations, d.CreatedAt
            ), ct);

            if (!res.Matched) { rejected++; continue; }

            matched++;
            if (res.Applied) applied++;
            else if (string.Equals(res.Reason, "Already applied", StringComparison.OrdinalIgnoreCase)) already++;
            else rejected++;
        }

        return new ApplyDepositsBatchResult(
            Total: request.Deposits.Count,
            Matched: matched,
            Applied: applied,
            AlreadyApplied: already,
            Rejected: rejected
        );
    }
}
