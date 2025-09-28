namespace GatewayService.AccountCharge.Application.Abstractions;

public interface IUnitOfWork
{
    // Commit changes in a single atomic transaction.
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    void ClearChangeTracker();       

}
