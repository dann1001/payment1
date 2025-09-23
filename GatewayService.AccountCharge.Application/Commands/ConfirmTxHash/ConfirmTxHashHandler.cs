// Application/Commands/ConfirmTxHash/ConfirmTxHashHandler.cs
using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Application.DTOs;
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

    // می‌تونی این‌ها را از کانفیگ بگیری
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

        var txHash = request.TxHash.Trim();
        if (txHash.Length == 0)
            return new ConfirmTxHashResult(invoice.Id, false, false, false, "Empty txHash");

        var walletIds = invoice.Addresses.Select(a => a.WalletId).Distinct().ToArray();
        if (walletIds.Length == 0)
            return new ConfirmTxHashResult(invoice.Id, false, false, false, "Invoice has no wallet/addresses");

        // از ۷ روز گذشته بگیر (می‌تونی null بدی اگر همه‌ی تاریخ را می‌خوای)
        var since = DateTimeOffset.UtcNow - Lookback;

        IncomingDepositDto? found = null;
        foreach (var wid in walletIds)
        {
            // ابستراکشن فعلی: (walletId, limit, since)
            var deposits = await _nobitex.GetRecentDepositsAsync(wid, FetchLimitPerWallet, since, ct);

            // مقایسه‌ی Case-insensitive روی TxHash
            found = deposits.FirstOrDefault(d =>
                string.Equals(d.TxHash?.Trim(), txHash, StringComparison.OrdinalIgnoreCase));

            if (found is not null)
                break;
        }

        if (found is null)
            return new ConfirmTxHashResult(invoice.Id, FoundOnExchange: false, Matched: false, Applied: false, Reason: "Not found");

        // حالا تلاش برای Apply به همین اینوویس
        var addr = new ChainAddress(found.Address, found.Network, found.Tag);

        var apply = await _mediator.Send(new ApplyDeposit.ApplyDepositCommand(
            TxHash: found.TxHash,
            Address: addr.Address,
            Network: addr.Network,
            Tag: addr.Tag,
            Amount: found.Amount,
            Currency: found.Currency,                 // طبق DTO: lowercase
            Confirmed: found.Confirmed,
            Confirmations: found.Confirmations,
            RequiredConfirmations: found.RequiredConfirmations,
            CreatedAt: found.CreatedAt
        ), ct);

        return new ConfirmTxHashResult(
            invoice.Id,
            FoundOnExchange: true,
            Matched: apply.Matched,   // فقط اگر آدرس/شبکه/تگ به یکی از Addressهای همین اینوویس بخوره
            Applied: apply.Applied,   // اگر قبلاً اعمال نشده باشد → true
            Reason: apply.Reason
        );
    }
}
