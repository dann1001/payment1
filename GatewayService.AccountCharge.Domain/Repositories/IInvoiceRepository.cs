using GatewayService.AccountCharge.Domain.Invoices;
using GatewayService.AccountCharge.Domain.ValueObjects;

namespace GatewayService.AccountCharge.Domain.Repositories;

/// <summary>
/// Repository abstraction for Invoice aggregate.
/// Implement address→invoice index in infrastructure for fast lookups.
/// </summary>
public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Efficient invoice lookup by a deposit address (for polling pipeline).
    /// Should use a persisted Address->Invoice map/table.
    /// </summary>
    Task<Invoice?> GetByAddressAsync(ChainAddress address, CancellationToken ct = default);

    /// <summary>
    /// Optional: get by invoice number shown to user.
    /// </summary>
    Task<Invoice?> GetByNumberAsync(string invoiceNumber, CancellationToken ct = default);

    Task AddAsync(Invoice invoice, CancellationToken ct = default);

    Task UpdateAsync(Invoice invoice, CancellationToken ct = default);
}
