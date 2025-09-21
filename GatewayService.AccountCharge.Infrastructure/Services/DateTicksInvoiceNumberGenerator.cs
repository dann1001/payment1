using GatewayService.AccountCharge.Application.Abstractions;

namespace GatewayService.AccountCharge.Infrastructure.Services;

public sealed class DateTicksInvoiceNumberGenerator : IInvoiceNumberGenerator
{
    public string Next()
    {
        // Example: INV-2025-09-17-638...-XYZ
        var now = DateTime.UtcNow;
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"INV-{now:yyyyMMdd}-{now.Ticks}-{suffix}";
    }
}
