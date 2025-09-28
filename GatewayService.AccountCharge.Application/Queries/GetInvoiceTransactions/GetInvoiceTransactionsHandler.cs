using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Application.DTOs;
using GatewayService.AccountCharge.Application.Queries.GetInvoiceTransactions;
using GatewayService.AccountCharge.Domain.Invoices;
using GatewayService.AccountCharge.Domain.Repositories;
using GatewayService.AccountCharge.Domain.ValueObjects;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayService.AccountCharge.Application.Queries.GetInvoiceTransactions;

public sealed class GetInvoiceTransactionsHandler
    : IRequestHandler<GetInvoiceTransactionsQuery, IReadOnlyList<IncomingDepositDto>>
{
    private const int MaxLimit = 200;
    private static readonly TimeSpan DefaultLookback = TimeSpan.FromDays(7);

    private readonly IInvoiceRepository _repo;
    private readonly INobitexClient _nobitex;

    public GetInvoiceTransactionsHandler(IInvoiceRepository repo, INobitexClient nobitex)
    {
        _repo = repo;
        _nobitex = nobitex;
    }

    public async Task<IReadOnlyList<IncomingDepositDto>> Handle(GetInvoiceTransactionsQuery request, CancellationToken ct)
    {
        var invoice = await _repo.GetByIdAsync(request.InvoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        var walletIds = invoice.Addresses.Select(a => a.WalletId).Distinct().ToArray();
        if (walletIds.Length == 0)
            return Array.Empty<IncomingDepositDto>();

        var since = request.SinceUtc ?? DateTimeOffset.UtcNow - DefaultLookback;
        var limit = Math.Clamp(request.Limit ?? MaxLimit, 1, MaxLimit);

        var results = new List<IncomingDepositDto>(capacity: walletIds.Length * 32);

        foreach (var wid in walletIds)
        {
            var deposits = await _nobitex.GetRecentDepositsAsync(wid, limit, since, ct);
            results.AddRange(deposits);
        }

        // Distinct by TxHash (case-insensitive)
        var distinct = results
            .Where(d => !string.IsNullOrWhiteSpace(d.TxHash))
            .GroupBy(d => d.TxHash!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.CreatedAt).First()) // latest snapshot per tx
            .ToList();

        if (request.OnlyInvoiceAddresses)
        {
            // Keep only deposits whose (address, network, tag) belongs to this invoice
            bool Owns(Invoice inv, IncomingDepositDto dep)
            {
                var addr = new ChainAddress(dep.Address, dep.Network, dep.Tag);
                return inv.OwnsAddress(addr);
            }

            distinct = distinct.Where(d => Owns(invoice, d)).ToList();
        }

        // Sort desc by time
        return distinct
            .OrderByDescending(d => d.CreatedAt)
            .ToList();
    }
}
