using System;
using System.Linq;
using System.Linq.Expressions;
using GatewayService.AccountCharge.Domain.Invoices;
using GatewayService.AccountCharge.Domain.Repositories;
using GatewayService.AccountCharge.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace GatewayService.AccountCharge.Infrastructure.Repositories;

public sealed class EfInvoiceRepository : IInvoiceRepository
{
    private readonly Persistence.AccountChargeDb _db;

    public EfInvoiceRepository(Persistence.AccountChargeDb db) => _db = db;

    public async Task AddAsync(Invoice invoice, CancellationToken ct = default)
        => await _db.Invoices.AddAsync(invoice, ct);

    public Task UpdateAsync(Invoice invoice, CancellationToken ct = default)
    {
        // Safer: if already tracked, do nothing. If detached, just Attach (no full Update)
        var entry = _db.Entry(invoice);
        if (entry.State == EntityState.Detached)
        {
            _db.Invoices.Attach(invoice);
            // rely on tracked changes; don't mark Modified blindly to avoid false concurrency hits
        }
        return Task.CompletedTask;
    }

    // For write scenarios use tracking (includes children)
    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Invoices
            .AsTracking()
            .Include(i => i.Addresses)
            .Include(i => i.AppliedDeposits)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    // Read-only (for query/display)
    public async Task<Invoice?> GetByNumberAsync(string invoiceNumber, CancellationToken ct = default)
    {
        return await _db.Invoices
            .AsNoTracking()
            .Include(i => i.Addresses)
            .Include(i => i.AppliedDeposits)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, ct);
    }

    public async Task<Invoice?> GetByAddressAsync(ChainAddress chainAddress, CancellationToken ct = default)
    {
        // Pre-normalize the constants once (client-side)
        var addr = chainAddress.Address.ToLower();                 // NOTE: not ToLowerInvariant()
        var net = (chainAddress.Network ?? string.Empty).ToLower();
        var tag = chainAddress.Tag; // keep nullable as-is

        return await _db.Invoices
            .AsTracking()
            .Include(i => i.Addresses)
            .Include(i => i.AppliedDeposits)
            .FirstOrDefaultAsync(i =>
               i.Addresses.Any(a =>
  a.Address.ToLower() == addr &&
  (string.IsNullOrEmpty(net) || (a.Network ?? "").ToLower() == net) &&
  ((a.Tag == null && tag == null) || a.Tag == tag)
)
, ct);
    }


    public async Task<bool> ExistsAsync(Expression<Func<Invoice, bool>> predicate, CancellationToken ct = default)
        => await _db.Invoices.AnyAsync(predicate, ct);
}
