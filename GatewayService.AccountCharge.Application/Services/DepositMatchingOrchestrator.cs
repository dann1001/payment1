using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Application.DTOs;
using GatewayService.AccountCharge.Domain.Invoices;
using GatewayService.AccountCharge.Domain.Repositories;
using GatewayService.AccountCharge.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace GatewayService.AccountCharge.Application.Services;

public sealed class DepositMatchingOrchestrator
{
    private readonly INobitexClient _nobitex;
    private readonly IInvoiceRepository _invoices;
    private readonly IPaymentMatchingOptionsProvider _opts;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DepositMatchingOrchestrator> _log;

    public DepositMatchingOrchestrator(
        INobitexClient nobitex,
        IInvoiceRepository invoices,
        IPaymentMatchingOptionsProvider opts,
        IUnitOfWork uow,
        ILogger<DepositMatchingOrchestrator> log)
    {
        _nobitex = nobitex;
        _invoices = invoices;
        _opts = opts;
        _uow = uow;
        _log = log;
    }

    public async Task<int> SyncInvoiceAsync(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await _invoices.GetByIdAsync(invoiceId, ct)
                      ?? throw new InvalidOperationException("Invoice not found.");

        int applied = 0;

        foreach (var addr in invoice.Addresses)
        {
            // invoice.CreatedAt = DateTime → convert to DateTimeOffset (UTC)
            var since = new DateTimeOffset(invoice.CreatedAt, TimeSpan.Zero);

            // pull recent deposits for this wallet
            var deposits = await _nobitex.GetRecentDepositsAsync(addr.WalletId, limit: 30, since: since, ct);

            foreach (var d in deposits)
            {
                // currency guard
                if (!string.Equals(d.Currency, invoice.ExpectedAmount.Currency, StringComparison.OrdinalIgnoreCase))
                    continue;

                // tag/memo guard when our stored address has a tag
                if (!string.IsNullOrWhiteSpace(addr.Tag) &&
                    !string.Equals(addr.Tag, d.Tag ?? string.Empty, StringComparison.Ordinal))
                    continue;

                // build Value Objects for domain
                var chain = new ChainAddress(d.Address, d.Network ?? string.Empty, d.Tag);
                var money = new Money(d.Amount, d.Currency);
                var pm = _opts.Get(d.Currency, d.Network ?? string.Empty);

                var incoming = new IncomingDeposit(
                    new TransactionHash(d.TxHash),
                    chain,
                    money,
                    d.Confirmed,
                    d.Confirmations,
                    d.RequiredConfirmations,
                    d.CreatedAt // DateTimeOffset
                );

                var ok = invoice.TryApplyDeposit(incoming, pm, out var reason);

                // count only real applies (idempotent "Already applied" را نشمار)
                if (ok && !string.Equals(reason, "Already applied", StringComparison.OrdinalIgnoreCase))
                    applied++;
            }
        }

        await _uow.SaveChangesAsync(ct);
        return applied;
    }

}
