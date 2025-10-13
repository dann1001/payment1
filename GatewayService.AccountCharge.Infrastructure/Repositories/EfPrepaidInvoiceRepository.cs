using GatewayService.AccountCharge.Domain.PrepaidInvoices;
using GatewayService.AccountCharge.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GatewayService.AccountCharge.Infrastructure.Repositories;

public sealed class EfPrepaidInvoiceRepository : IPrepaidInvoiceRepository
{
    private readonly Persistence.AccountChargeDb _db;
    public EfPrepaidInvoiceRepository(Persistence.AccountChargeDb db) => _db = db;

    public async Task AddAsync(PrepaidInvoice entity, CancellationToken ct = default)
        => await _db.PrepaidInvoices.AddAsync(entity, ct);

    public Task UpdateAsync(PrepaidInvoice entity, CancellationToken ct = default)
    {
        var e = _db.Entry(entity);
        if (e.State == EntityState.Detached) _db.PrepaidInvoices.Attach(entity);
        return Task.CompletedTask;
    }

    public Task<PrepaidInvoice?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.PrepaidInvoices.AsTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<PrepaidInvoice?> GetByTxHashAsync(string txHash, CancellationToken ct = default)
        => _db.PrepaidInvoices.AsNoTracking().FirstOrDefaultAsync(x => x.TxHash == txHash, ct);

    public Task<bool> ExistsByTxHashAsync(string txHash, CancellationToken ct = default)
        => _db.PrepaidInvoices.AsNoTracking().AnyAsync(x => x.TxHash == txHash, ct);
}
