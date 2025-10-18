// File: D:\GatewayService.AccountCharge\GatewayService.AccountCharge.Application\Commands\ApplyDeposit\ApplyDepositHandler.cs
// D:\GatewayService.AccountCharge\GatewayService.AccountCharge.Application\Commands\ApplyDeposit\ApplyDepositToInvoiceHandler.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Application.Commands.ApplyDeposit;
using GatewayService.AccountCharge.Domain.Invoices;
using GatewayService.AccountCharge.Domain.Repositories;
using GatewayService.AccountCharge.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GatewayService.AccountCharge.Application.Commands.ApplyDeposit
{
    public sealed class ApplyDepositToInvoiceHandler
        : IRequestHandler<ApplyDepositToInvoiceCommand, ApplyDepositResult>
    {
        private readonly IInvoiceRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly IPaymentMatchingOptionsProvider _optsProvider;
        private readonly IAccountingClient _accounting;
        private readonly IPriceQuoteClient _prices;                         // ⬅️ اضافه شد
        private readonly ILogger<ApplyDepositToInvoiceHandler> _logger;

        public ApplyDepositToInvoiceHandler(
            IInvoiceRepository repo,
            IUnitOfWork uow,
            IPaymentMatchingOptionsProvider optsProvider,
            IAccountingClient accounting,
            IPriceQuoteClient prices,                                       // ⬅️ اضافه شد
            ILogger<ApplyDepositToInvoiceHandler> logger)
        {
            _repo = repo;
            _uow = uow;
            _optsProvider = optsProvider;
            _accounting = accounting;
            _prices = prices;                                               // ⬅️ اضافه شد
            _logger = logger;
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

                // ---- Convert to USDT (only if needed) & call Accounting ----
                try
                {
                    Guid externalCustomerId;
                    var prop = invoice.GetType().GetProperty("CustomerId");
                    var raw = prop?.GetValue(invoice);

                    if (raw is Guid gid) externalCustomerId = gid;
                    else if (raw is string s && Guid.TryParse(s, out var g2)) externalCustomerId = g2;
                    else throw new InvalidOperationException("Invoice.CustomerId must be a Guid (or Guid string).");

                    var srcCurrency = request.Currency.Trim().ToUpperInvariant();
                    decimal usdtAmount;

                    if (srcCurrency == "USDT")
                    {
                        usdtAmount = request.Amount;
                    }
                    else
                    {
                        var rate = await _prices.GetUsdtQuoteAsync(srcCurrency, request.CreatedAt, ct);
                        usdtAmount = decimal.Round(request.Amount * rate, 6, MidpointRounding.ToEven);
                    }

                    await _accounting.CreateDepositAsync(
                        externalCustomerId: externalCustomerId,
                        amount: usdtAmount,
                        currency: "USDT",
                        occurredAt: request.CreatedAt,
                        idempotencyKey: request.TxHash,
                        ct: ct
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Accounting (USDT) post failed for Invoice {InvoiceId} / Tx {TxHash}. State committed.",
                        invoice.Id, request.TxHash);
                    // swallow; state already committed
                }
                // -----------------------------------------------------------

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
}
