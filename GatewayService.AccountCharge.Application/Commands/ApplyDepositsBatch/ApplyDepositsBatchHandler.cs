using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.ApplyDeposit;

public sealed class ApplyDepositsBatchHandler
    : IRequestHandler<ApplyDepositsBatchCommand, ApplyDepositsBatchResult>
{
    private readonly IMediator _mediator;

    public ApplyDepositsBatchHandler(IMediator mediator) => _mediator = mediator;

    public async Task<ApplyDepositsBatchResult> Handle(ApplyDepositsBatchCommand request, CancellationToken ct)
    {
        int matched = 0, applied = 0, already = 0, rejected = 0;

        foreach (var d in request.Deposits)
        {
            var res = await _mediator.Send(new ApplyDepositToInvoiceCommand(
                InvoiceId: d.InvoiceId,   // ✅ الان داریم
                TxHash: d.TxHash,
                Address: d.Address,
                Network: d.Network,
                Tag: d.Tag,
                Amount: d.Amount,
                Currency: d.Currency,
                Confirmed: d.Confirmed,
                Confirmations: d.Confirmations,
                RequiredConfirmations: d.RequiredConfirmations,
                CreatedAt: d.CreatedAt
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
