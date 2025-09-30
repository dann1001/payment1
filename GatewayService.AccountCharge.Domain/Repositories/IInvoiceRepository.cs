using GatewayService.AccountCharge.Domain.Invoices;
using GatewayService.AccountCharge.Domain.ValueObjects;
using System.Linq.Expressions;

namespace GatewayService.AccountCharge.Domain.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Invoice?> GetByAddressAsync(ChainAddress address, CancellationToken ct = default); // legacy
    Task<Invoice?> GetByNumberAsync(string invoiceNumber, CancellationToken ct = default);

    Task AddAsync(Invoice invoice, CancellationToken ct = default);
    Task UpdateAsync(Invoice invoice, CancellationToken ct = default);

    Task<bool> HasAppliedDepositAsync(Guid invoiceId, string txHash, CancellationToken ct = default);

    // ✅ add this:
    Task<bool> HasAnyAppliedDepositAsync(string txHash, CancellationToken ct = default);

    Task<bool> ExistsAsync(Expression<Func<Invoice, bool>> predicate, CancellationToken ct = default);
}
