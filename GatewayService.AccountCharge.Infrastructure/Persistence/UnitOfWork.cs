using GatewayService.AccountCharge.Application.Abstractions;

namespace GatewayService.AccountCharge.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AccountChargeDb _db;

    public UnitOfWork(AccountChargeDb db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
