using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Application.DTOs;
using GatewayService.AccountCharge.Application.Commands.ApplyDeposit;
using GatewayService.AccountCharge.Domain.Repositories;
using GatewayService.AccountCharge.Domain.ValueObjects;
using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.ConfirmTxHash;

public sealed class ConfirmTxHashHandler
    : IRequestHandler<ConfirmTxHashCommand, ConfirmTxHashResult>
{
    private readonly IInvoiceRepository _repo;
    private readonly INobitexClient _nobitex;
    private readonly IMediator _mediator;

    private const int FetchLimitPerWallet = 200;
    private static readonly TimeSpan Lookback = TimeSpan.FromDays(7);

    public ConfirmTxHashHandler(IInvoiceRepository repo, INobitexClient nobitex, IMediator mediator)
    {
        _repo = repo;
        _nobitex = nobitex;
        _mediator = mediator;
    }

    public async Task<ConfirmTxHashResult> Handle(ConfirmTxHashCommand request, CancellationToken ct)
    {
        var invoice = await _repo.GetByIdAsync(request.InvoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        var txHashStr = request.TxHash?.Trim();
        if (string.IsNullOrWhiteSpace(txHashStr))
            return new ConfirmTxHashResult(invoice.Id, false, false, false, "Empty txHash");

        // Global guard: already applied anywhere?
        if (await _repo.HasAnyAppliedDepositAsync(txHashStr, ct))
            return new ConfirmTxHashResult(invoice.Id, true, false, false, "Already applied (global)");

        var walletIds = invoice.Addresses.Select(a => a.WalletId).Distinct().ToArray();
        if (walletIds.Length == 0)
            return new ConfirmTxHashResult(invoice.Id, false, false, false, "Invoice has no wallet/addresses");

        var since = DateTimeOffset.UtcNow - Lookback;

        IncomingDepositDto? found = null;
        foreach (var wid in walletIds)
        {
            var deposits = await _nobitex.GetRecentDepositsAsync(wid, FetchLimitPerWallet, since, ct);
            found = deposits.FirstOrDefault(d =>
                string.Equals(d.TxHash?.Trim(), txHashStr, StringComparison.OrdinalIgnoreCase));
            if (found is not null) break;
        }

        if (found is null)
            return new ConfirmTxHashResult(invoice.Id, false, false, false, "Not found");

        var apply = await _mediator.Send(new ApplyDepositToInvoiceCommand(
            InvoiceId: invoice.Id,
            TxHash: found.TxHash!,
            Address: found.Address,
            Network: found.Network,
            Tag: found.Tag,
            Amount: found.Amount,
            Currency: found.Currency,
            Confirmed: found.Confirmed,
            Confirmations: found.Confirmations,
            RequiredConfirmations: found.RequiredConfirmations,
            CreatedAt: found.CreatedAt
        ), ct);

        return new ConfirmTxHashResult(invoice.Id, true, apply.Matched, apply.Applied, apply.Reason);
    }
}
