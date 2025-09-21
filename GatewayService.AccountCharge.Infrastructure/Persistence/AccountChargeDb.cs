using Microsoft.EntityFrameworkCore;

namespace GatewayService.AccountCharge.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext wired to Domain aggregates (persistence-ignorant).
/// </summary>
public sealed class AccountChargeDb : DbContext
{
    public AccountChargeDb(DbContextOptions<AccountChargeDb> options) : base(options) { }

    // Aggregates
    public DbSet<Domain.Invoices.Invoice> Invoices => Set<Domain.Invoices.Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccountChargeDb).Assembly);
    }
}
