// D:\GatewayService.AccountCharge\GatewayService.AccountCharge.Application\Commands\ApplyDeposit\ApplyDepositToInvoiceHandler.cs
using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Application.Commands.ApplyDeposit;
using GatewayService.AccountCharge.Domain.Invoices;
using GatewayService.AccountCharge.Domain.Repositories;
using GatewayService.AccountCharge.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
// ...existing usings

public sealed class ApplyDepositToInvoiceHandler
    : IRequestHandler<ApplyDepositToInvoiceCommand, ApplyDepositResult>
{
    private readonly IInvoiceRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IPaymentMatchingOptionsProvider _optsProvider;
    private readonly IAccountingClient _accounting;                     // <-- ADD
    private readonly ILogger<ApplyDepositToInvoiceHandler> _logger;     // <-- ADD

    public ApplyDepositToInvoiceHandler(
        IInvoiceRepository repo,
        IUnitOfWork uow,
        IPaymentMatchingOptionsProvider optsProvider,
        IAccountingClient accounting,                                    // <-- ADD
        ILogger<ApplyDepositToInvoiceHandler> logger)                    // <-- ADD
    {
        _repo = repo;
        _uow = uow;
        _optsProvider = optsProvider;
        _accounting = accounting;                                        // <-- ADD
        _logger = logger;                                                // <-- ADD
    }

    public async Task<ApplyDepositResult> Handle(ApplyDepositToInvoiceCommand request, CancellationToken ct)
    {
        _uow.ClearChangeTracker();

        var invoice = await _repo.GetByIdAsync(request.InvoiceId, ct);
        if (invoice is null)
            return new ApplyDepositResult(false, false, "Invoice not found", null);

        var txHash = new TransactionHash(request.TxHash);

        if (await _repo.HasAnyAppliedDepositAsync(txHash.Value, ct))
            return new ApplyDepositResult(true, false, "Already applied (global)", invoice.Id);

        if (await _repo.HasAppliedDepositAsync(invoice.Id, txHash.Value, ct))
            return new ApplyDepositResult(true, false, "Already applied", invoice.Id);

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

        var ok = invoice.TryApplyDeposit(incoming, opts, out var reason);
        if (!ok)
            return new ApplyDepositResult(true, false, reason, invoice.Id);

        try
        {
            await _repo.UpdateAsync(invoice, ct);
            await _uow.SaveChangesAsync(ct); // ✅ committed

            // ---- CALL ACCOUNTING (fire-and-forget semantics) ----
            try
            {
                // Try to extract ExternalCustomerId from invoice.CustomerId (Guid or string Guid)
                Guid externalCustomerId;

                var prop = invoice.GetType().GetProperty("CustomerId");
                var raw = prop?.GetValue(invoice);

                if (raw is Guid gid)
                {
                    externalCustomerId = gid;
                }
                else if (raw is string s && Guid.TryParse(s, out var g2))
                {
                    externalCustomerId = g2;
                }
                else
                {
                    throw new InvalidOperationException("Invoice.CustomerId must be a Guid (or Guid string).");
                }

                await _accounting.CreateDepositAsync(
                    externalCustomerId: externalCustomerId,
                    amount: request.Amount,
                    currency: request.Currency.ToUpperInvariant(),
                    occurredAt: request.CreatedAt,
                    idempotencyKey: request.TxHash,     // helps prevent duplicates if you add support serverside
                    ct: ct
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Accounting post failed for Invoice {InvoiceId} / Tx {TxHash}. " +
                    "Invoice already updated — please retry out-of-band.",
                    invoice.Id, request.TxHash);
                // Intentionally swallow: invoice state is authoritative here.
            }
            // ------------------------------------------------------

            return new ApplyDepositResult(true, true, reason, invoice.Id);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
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
