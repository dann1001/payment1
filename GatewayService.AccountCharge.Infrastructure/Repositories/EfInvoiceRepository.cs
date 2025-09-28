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
        var addr = chainAddress.Address.ToLower();
        var net = (chainAddress.Network ?? string.Empty).ToLower();
        var tag = chainAddress.Tag;

        var invoiceId = await _db.Invoices
            .AsNoTracking()
            .SelectMany(i => i.Addresses, (i, a) => new { i.Id, a })
            .Where(x =>
                x.a.Address.ToLower() == addr &&
                (string.IsNullOrEmpty(net) || (x.a.Network ?? "").ToLower() == net) &&
                ((x.a.Tag == null && tag == null) || x.a.Tag == tag))
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (invoiceId == Guid.Empty)
            return null;

        var tracked = _db.ChangeTracker.Entries<Invoice>().FirstOrDefault(e => e.Entity.Id == invoiceId);
        if (tracked is not null) tracked.State = EntityState.Detached;

        return await _db.Invoices
            .AsTracking()
            .Include(i => i.Addresses)
            .Include(i => i.AppliedDeposits)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
    }


    // Infrastructure/Repositories/EfInvoiceRepository.cs
    public async Task<bool> HasAppliedDepositAsync(Guid invoiceId, string txHash, CancellationToken ct = default)
    {
        var th = new TransactionHash(txHash); // let EF apply the ValueConverter

        // Filter inside SelectMany and avoid string methods on VO
        return await _db.Invoices
            .AsNoTracking()
            .Where(i => i.Id == invoiceId)
            .SelectMany(i => i.AppliedDeposits.Where(d => d.TxHash == th))
            .AnyAsync(ct);
    }



    public async Task<bool> ExistsAsync(Expression<Func<Invoice, bool>> predicate, CancellationToken ct = default)
        => await _db.Invoices.AnyAsync(predicate, ct);
}
