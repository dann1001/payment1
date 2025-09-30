using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Domain.Invoices;
using GatewayService.AccountCharge.Domain.Repositories;
using GatewayService.AccountCharge.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GatewayService.AccountCharge.Application.Commands.ApplyDeposit;

public sealed class ApplyDepositToInvoiceHandler
    : IRequestHandler<ApplyDepositToInvoiceCommand, ApplyDepositResult>
{
    private readonly IInvoiceRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IPaymentMatchingOptionsProvider _optsProvider;

    public ApplyDepositToInvoiceHandler(IInvoiceRepository repo, IUnitOfWork uow, IPaymentMatchingOptionsProvider optsProvider)
    {
        _repo = repo;
        _uow = uow;
        _optsProvider = optsProvider;
    }

    public async Task<ApplyDepositResult> Handle(ApplyDepositToInvoiceCommand request, CancellationToken ct)
    {
        _uow.ClearChangeTracker();

        var invoice = await _repo.GetByIdAsync(request.InvoiceId, ct);
        if (invoice is null)
            return new ApplyDepositResult(false, false, "Invoice not found", null);

        var txHash = new TransactionHash(request.TxHash);

        // 🔒 Global guard: if already applied anywhere, stop
        if (await _repo.HasAnyAppliedDepositAsync(txHash.Value, ct))
            return new ApplyDepositResult(true, false, "Already applied (global)", invoice.Id);

        // DB guard for this invoice (idempotency)
        if (await _repo.HasAppliedDepositAsync(invoice.Id, txHash.Value, ct))
            return new ApplyDepositResult(true, false, "Already applied", invoice.Id);

        // Build incoming
        var incoming = new IncomingDeposit(
            txHash,
            new ChainAddress(request.Address, request.Network, request.Tag),
            new Money(request.Amount, request.Currency),
            request.Confirmed,
            request.Confirmations,
            request.RequiredConfirmations,
            request.CreatedAt
        );

        var opts = _optsProvider.Get(request.Currency, request.Network);

        // 💡 With static address, set RequireKnownAddress=false in config
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
            // Likely hit the global unique TxHash index
            return new ApplyDepositResult(true, false, "Already applied (db-unique)", invoice.Id);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var msg = (ex.InnerException?.Message ?? ex.Message) ?? string.Empty;
        return msg.IndexOf("UNIQUE", StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("duplicate", StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("IX_InvoiceAppliedDeposits_TxHash", StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("UQ__InvoiceAppliedDeposits__TxHash", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
