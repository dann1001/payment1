using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Application.DTOs;
using GatewayService.AccountCharge.Domain.Invoices;
using GatewayService.AccountCharge.Domain.Repositories;
using GatewayService.AccountCharge.Domain.ValueObjects;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayService.AccountCharge.Application.Queries.GetInvoiceTransactionsAll;

public sealed class GetInvoiceTransactionsAllHandler
    : IRequestHandler<GetInvoiceTransactionsAllQuery, IReadOnlyList<IncomingDepositDto>>
{
    // 🔧 Hard-coded defaults (tune as needed)
    private const int PAGE_SIZE = 200;                 // per-wallet pull cap
    private static readonly TimeSpan LOOKBACK = TimeSpan.FromDays(90);

    private readonly IInvoiceRepository _repo;
    private readonly INobitexClient _nobitex;

    public GetInvoiceTransactionsAllHandler(
        IInvoiceRepository repo,
        INobitexClient nobitex)
    {
        _repo = repo;
        _nobitex = nobitex;
    }

    public async Task<IReadOnlyList<IncomingDepositDto>> Handle(GetInvoiceTransactionsAllQuery request, CancellationToken ct)
    {
        var invoice = await _repo.GetByIdAsync(request.InvoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        var walletIds = invoice.Addresses.Select(a => a.WalletId).Distinct().ToArray();
        if (walletIds.Length == 0)
            return Array.Empty<IncomingDepositDto>();

        var cutoff = DateTimeOffset.UtcNow - LOOKBACK;
        var all = new List<IncomingDepositDto>(capacity: walletIds.Length * 256);

        // Pull once per walletId (bounded by PAGE_SIZE & LOOKBACK)
        foreach (var wid in walletIds)
        {
            var batch = await _nobitex.GetRecentDepositsAsync(wid, PAGE_SIZE, cutoff, ct);
            if (batch is { Count: > 0 })
                all.AddRange(batch);
        }

        // De-dup by txHash (case-insensitive), keep latest snapshot
        var distinct = all
            .Where(d => !string.IsNullOrWhiteSpace(d.TxHash))
            .GroupBy(d => d.TxHash!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.CreatedAt).First())
            .ToList();

        // Filter to addresses owned by this invoice (safe default)
        bool Owns(IncomingDepositDto dep)
        {
            var addr = new ChainAddress(dep.Address, dep.Network, dep.Tag);
            return invoice.OwnsAddress(addr);
        }
        distinct = distinct.Where(Owns).ToList();

        // newest → oldest
        return distinct
            .OrderByDescending(d => d.CreatedAt)
            .ToList();
    }
}
