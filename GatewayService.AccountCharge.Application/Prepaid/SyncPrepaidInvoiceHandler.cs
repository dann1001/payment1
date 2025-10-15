using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Application.Common;
using GatewayService.AccountCharge.Application.DTOs;
using GatewayService.AccountCharge.Domain.PrepaidInvoices;
using GatewayService.AccountCharge.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GatewayService.AccountCharge.Application.Commands.Prepaid;

public sealed class SyncPrepaidInvoiceHandler : IRequestHandler<SyncPrepaidInvoiceCommand, bool>
{
    private readonly IPrepaidInvoiceRepository _repo;
    private readonly IInvoiceRepository _legacyInvoices; // برای جلوگیری از double-apply
    private readonly INobitexClient _nobitex;
    private readonly IPaymentMatchingOptionsProvider _opts;
    private readonly IAccountingClient _accounting;
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
        IUnitOfWork uow,
        ILogger<SyncPrepaidInvoiceHandler> log)
    {
        _repo = repo;
        _legacyInvoices = legacyInvoices;
        _nobitex = nobitex;
        _opts = opts;
        _accounting = accounting;
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

        // ❗به‌جای رد کردن، فقط Accounting را بعداً Skip می‌کنیم
        var alreadyAppliedOnLegacy = await _legacyInvoices.HasAnyAppliedDepositAsync(p.TxHash, ct);
        if (alreadyAppliedOnLegacy)
            _log.LogInformation("PrepaidInvoice {Id}: Tx {Tx} already applied on legacy. Will skip Accounting but proceed to mark status.", p.Id, p.TxHash);

        // Wallets با ارز موردنظر
        var wallets = await _nobitex.GetWalletsAsync(ct);
        var wantedSymbol = AssetMapper.NormalizeCurrency(p.Currency); // upper
        var candidateWallets = wallets.Where(w => AssetMapper.NormalizeCurrency(w.Currency) == wantedSymbol).ToList();

        if (candidateWallets.Count == 0)
            throw new InvalidOperationException($"No Nobitex wallet for currency {p.Currency}");

        // جست‌وجوی تراکنش
        IncomingDepositDto? dep = null;
        foreach (var w in candidateWallets)
        {
            var since = p.CreatedAt - LOOKBACK;
            var list = await _nobitex.GetRecentDepositsAsync(w.Id, FETCH_LIMIT, since, ct);
            dep = list.FirstOrDefault(d => string.Equals(d.TxHash?.Trim(), p.TxHash, StringComparison.OrdinalIgnoreCase));
            if (dep is not null)
            {
                _log.LogInformation("PrepaidInvoice {Id}: Found deposit (wallet {WalletId}) {Amount} {Currency} at {CreatedAt:o}",
                    p.Id, w.Id, dep.Amount, dep.Currency, dep.CreatedAt);
                break;
            }
        }

        if (dep is null)
        {
            // پیدا نشد → فقط LastCheckedAt آپدیت می‌ماند
            await _repo.UpdateAsync(p, ct);
            await _uow.SaveChangesAsync(ct);
            return before != p.Status;
        }

        // تطبیق ارز
        if (!string.Equals(dep.Currency, p.Currency, StringComparison.OrdinalIgnoreCase))
        {
            p.MarkRejectedCurrencyMismatch(dep.Currency, dep.CreatedAt);
            await _repo.UpdateAsync(p, ct);
            await _uow.SaveChangesAsync(ct);
            return before != p.Status;
        }

        // ❌ بررسی آدرس حذف شد (طبق توافق)

        // کانفرمیشن‌ها
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

        // ✅ Paid
        p.MarkPaid(dep.Amount, dep.Currency, dep.Address, dep.Tag,
                   dep.WalletId == 0 ? null : dep.WalletId, dep.Confirmations, dep.RequiredConfirmations, dep.CreatedAt);

        await _repo.UpdateAsync(p, ct);
        await _uow.SaveChangesAsync(ct);

        // Accounting فقط اگر قبلاً روی legacy اعمال نشده باشد
        if (!alreadyAppliedOnLegacy)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(p.CustomerId) && Guid.TryParse(p.CustomerId, out var ext))
                {
                    await _accounting.CreateDepositAsync(
                        externalCustomerId: ext,
                        amount: dep.Amount,
                        currency: dep.Currency.ToUpperInvariant(),
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
                _log.LogError(ex, "Accounting sync failed for PrepaidInvoice {Id} (Tx {Tx})", p.Id, p.TxHash);
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
