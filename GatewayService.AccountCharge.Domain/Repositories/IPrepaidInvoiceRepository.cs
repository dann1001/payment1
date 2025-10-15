using GatewayService.AccountCharge.Domain.PrepaidInvoices;

namespace GatewayService.AccountCharge.Domain.Repositories;

public interface IPrepaidInvoiceRepository
{
    Task AddAsync(PrepaidInvoice entity, CancellationToken ct = default);
    Task UpdateAsync(PrepaidInvoice entity, CancellationToken ct = default);
    Task<PrepaidInvoice?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PrepaidInvoice?> GetByTxHashAsync(string txHash, CancellationToken ct = default);
    Task<bool> ExistsByTxHashAsync(string txHash, CancellationToken ct = default);
}
