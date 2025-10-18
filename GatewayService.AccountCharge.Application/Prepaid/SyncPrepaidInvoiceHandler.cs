// File: D:\GatewayService.AccountCharge\GatewayService.AccountCharge.Application\Prepaid\SyncPrepaidInvoiceHandler.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Application.Common;
using GatewayService.AccountCharge.Application.DTOs;
using GatewayService.AccountCharge.Domain.PrepaidInvoices;
using GatewayService.AccountCharge.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GatewayService.AccountCharge.Application.Commands.Prepaid
{
    public sealed class SyncPrepaidInvoiceHandler : IRequestHandler<SyncPrepaidInvoiceCommand, bool>
    {
        private readonly IPrepaidInvoiceRepository _repo;
        private readonly IInvoiceRepository _legacyInvoices; // legacy skip
        private readonly INobitexClient _nobitex;
        private readonly IPaymentMatchingOptionsProvider _opts;
        private readonly IAccountingClient _accounting;
        private readonly IPriceQuoteClient _prices;          // ⬅️ اضافه شد
        private readonly IUnitOfWork _uow;
        private readonly ILogger<SyncPrepaidInvoiceHandler> _log;

        private const int FETCH_LIMIT = 200;
        private static readonly TimeSpan LOOKBACK = TimeSpan.FromDays(30);

        public SyncPrepaidInvoiceHandler(
            IPrepaidInvoiceRepository repo,
            IInvoiceRepository legacyInvoices,
            INobitexClient nobitex,
            IPaymentMatchingOptionsProvider opts,
            IAccountingClient accounting,
            IPriceQuoteClient prices,                         // ⬅️ اضافه شد
            IUnitOfWork uow,
            ILogger<SyncPrepaidInvoiceHandler> log)
        {
            _repo = repo;
            _legacyInvoices = legacyInvoices;
            _nobitex = nobitex;
            _opts = opts;
            _accounting = accounting;
            _prices = prices;                                 // ⬅️ اضافه شد
            _uow = uow;
            _log = log;
        }

        public async Task<bool> Handle(SyncPrepaidInvoiceCommand request, CancellationToken ct)
        {
            var p = await _repo.GetByIdAsync(request.Id, ct) ?? throw new KeyNotFoundException("PrepaidInvoice not found.");
            var before = p.Status;

            if (p.IsExpired())
            {
                p.MarkExpired();
                await _repo.UpdateAsync(p, ct);
                await _uow.SaveChangesAsync(ct);
                return before != p.Status;
            }

            var alreadyAppliedOnLegacy = await _legacyInvoices.HasAnyAppliedDepositAsync(p.TxHash, ct);
            if (alreadyAppliedOnLegacy)
                _log.LogInformation("PrepaidInvoice {Id}: Tx {Tx} already applied on legacy. Will skip Accounting.", p.Id, p.TxHash);

            var wallets = await _nobitex.GetWalletsAsync(ct);
            var wantedSymbol = AssetMapper.NormalizeCurrency(p.Currency); // upper
            var candidateWallets = wallets.Where(w => AssetMapper.NormalizeCurrency(w.Currency) == wantedSymbol).ToList();

            if (candidateWallets.Count == 0)
                throw new InvalidOperationException($"No Nobitex wallet for currency {p.Currency}");

            IncomingDepositDto? dep = null;
            foreach (var w in candidateWallets)
            {
                var since = p.CreatedAt - LOOKBACK;
                var list = await _nobitex.GetRecentDepositsAsync(w.Id, FETCH_LIMIT, since, ct);
                dep = list.FirstOrDefault(d => string.Equals(d.TxHash?.Trim(), p.TxHash, StringComparison.OrdinalIgnoreCase));
                if (dep is not null) break;
            }

            if (dep is null)
            {
                await _repo.UpdateAsync(p, ct);
                await _uow.SaveChangesAsync(ct);
                return before != p.Status;
            }

            if (!string.Equals(dep.Currency, p.Currency, StringComparison.OrdinalIgnoreCase))
            {
                p.MarkRejectedCurrencyMismatch(dep.Currency, dep.CreatedAt);
                await _repo.UpdateAsync(p, ct);
                await _uow.SaveChangesAsync(ct);
                return before != p.Status;
            }

            var opts = _opts.Get(dep.Currency, dep.Network ?? string.Empty);
            var required = Math.Max(opts.MinConfirmations, dep.RequiredConfirmations);
            if (dep.Confirmations < required)
            {
                p.MarkAwaiting(dep.Confirmations, dep.RequiredConfirmations, dep.Amount, dep.Currency,
                               dep.Address, dep.Tag, dep.WalletId == 0 ? null : dep.WalletId, dep.CreatedAt);
                await _repo.UpdateAsync(p, ct);
                await _uow.SaveChangesAsync(ct);
                return before != p.Status;
            }

            // Paid snapshot
            p.MarkPaid(dep.Amount, dep.Currency, dep.Address, dep.Tag,
                       dep.WalletId == 0 ? null : dep.WalletId, dep.Confirmations, dep.RequiredConfirmations, dep.CreatedAt);

            await _repo.UpdateAsync(p, ct);
            await _uow.SaveChangesAsync(ct);

            if (!alreadyAppliedOnLegacy)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(p.CustomerId) && Guid.TryParse(p.CustomerId, out var ext))
                    {
                        var src = dep.Currency.ToUpperInvariant();
                        decimal usdtAmount;

                        if (src == "USDT")
                        {
                            usdtAmount = dep.Amount;
                        }
                        else
                        {
                            var rate = await _prices.GetUsdtQuoteAsync(src, dep.CreatedAt, ct);
                            usdtAmount = decimal.Round(dep.Amount * rate, 6, MidpointRounding.ToEven);
                        }

                        await _accounting.CreateDepositAsync(
                            externalCustomerId: ext,
                            amount: usdtAmount,
                            currency: "USDT",
                            occurredAt: dep.CreatedAt,
                            idempotencyKey: p.TxHash,
                            ct: ct);
                    }
                    else
                    {
                        _log.LogWarning("PrepaidInvoice {Id}: CustomerId is not a Guid; skipping Accounting.", p.Id);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Accounting sync (USDT) failed for PrepaidInvoice {Id} (Tx {Tx})", p.Id, p.TxHash);
                    p.MarkAccountingSyncFailed();
                    await _repo.UpdateAsync(p, ct);
                    await _uow.SaveChangesAsync(ct);
                }
            }
            else
            {
                _log.LogInformation("PrepaidInvoice {Id}: Skipped Accounting because tx is already applied on legacy.", p.Id);
            }

            return before != p.Status;
        }
    }
}
