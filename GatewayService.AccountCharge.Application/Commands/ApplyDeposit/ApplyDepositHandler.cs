// Application/Commands/ApplyDeposit/ApplyDepositHandler.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Domain.Invoices;
using GatewayService.AccountCharge.Domain.Repositories;
using GatewayService.AccountCharge.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GatewayService.AccountCharge.Application.Commands.ApplyDeposit;

public sealed class ApplyDepositHandler : IRequestHandler<ApplyDepositCommand, ApplyDepositResult>
{
    private readonly IInvoiceRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IPaymentMatchingOptionsProvider _optsProvider;

    public ApplyDepositHandler(IInvoiceRepository repo, IUnitOfWork uow, IPaymentMatchingOptionsProvider optsProvider)
    {
        _repo = repo;
        _uow = uow;
        _optsProvider = optsProvider;
    }

    public async Task<ApplyDepositResult> Handle(ApplyDepositCommand request, CancellationToken ct)
    {
        // attempt 1 (fresh context state)
        _uow.ClearChangeTracker();

        var addr = new ChainAddress(request.Address, request.Network, request.Tag);
        var invoice = await _repo.GetByAddressAsync(addr, ct);
        if (invoice is null)
            return new ApplyDepositResult(false, false, "No invoice owns this address", null);

        var txHash = new TransactionHash(request.TxHash);

        // DB truth: if already applied, return success (idempotent)
        if (await _repo.HasAppliedDepositAsync(invoice.Id, txHash.Value, ct))
            return new ApplyDepositResult(true, false, "Already applied", invoice.Id);

        var incoming = new IncomingDeposit(
            txHash,
            addr,
            new Money(request.Amount, request.Currency), // ensure upstream normalization (AssetMapper)
            request.Confirmed,
            request.Confirmations,
            request.RequiredConfirmations,
            request.CreatedAt
        );

        var opts = _optsProvider.Get(request.Currency, request.Network);

        // Try in-memory apply
        var ok = invoice.TryApplyDeposit(incoming, opts, out var reason);
        if (!ok)
            return new ApplyDepositResult(true, false, reason, invoice.Id);

        try
        {
            await _repo.UpdateAsync(invoice, ct);
            await _uow.SaveChangesAsync(ct);
            return new ApplyDepositResult(true, reason != "Already applied", reason, invoice.Id);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Another writer won: DB has it.
            return new ApplyDepositResult(true, false, "Already applied", invoice.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            // 🔁 Retry once, fully reloading and re-applying
            _uow.ClearChangeTracker();

            // Check DB again before touching domain
            if (await _repo.HasAppliedDepositAsync(invoice.Id, txHash.Value, ct))
                return new ApplyDepositResult(true, false, "Already applied", invoice.Id);

            // Reload current invoice state
            var latest = await _repo.GetByIdAsync(invoice.Id, ct);
            if (latest is null)
                return new ApplyDepositResult(false, false, "Invoice vanished during save", null);

            var ok2 = latest.TryApplyDeposit(incoming, opts, out var reason2);
            if (!ok2)
                return new ApplyDepositResult(true, false, reason2, latest.Id);

            try
            {
                await _repo.UpdateAsync(latest, ct);
                await _uow.SaveChangesAsync(ct);
                return new ApplyDepositResult(true, reason2 != "Already applied", reason2, latest.Id);
            }
            catch (DbUpdateException ex2) when (IsUniqueViolation(ex2))
            {
                return new ApplyDepositResult(true, false, "Already applied", latest.Id);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Give a definitive, consistent answer
                _uow.ClearChangeTracker();
                var exists = await _repo.HasAppliedDepositAsync(invoice.Id, txHash.Value, ct);
                return exists
                    ? new ApplyDepositResult(true, false, "Already applied", invoice.Id)
                    : new ApplyDepositResult(false, false, "Concurrency race: not applied", invoice.Id);
            }
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var msg = (ex.InnerException?.Message ?? ex.Message) ?? string.Empty;
        return msg.IndexOf("UNIQUE", StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("duplicate", StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("IX_InvoiceAppliedDeposits_InvoiceId_TxHash", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
