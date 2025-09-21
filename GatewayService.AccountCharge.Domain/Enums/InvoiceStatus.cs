namespace GatewayService.AccountCharge.Domain.Enums;

public enum InvoiceStatus
{
    Pending = 0,        // No deposit matched yet
    PartiallyPaid = 1,  // Some deposit(s) matched but less than expected
    Paid = 2,           // Fully paid within tolerance
    Overpaid = 3,       // Paid more than expected (beyond tolerance)
    Expired = 4,        // Payment window elapsed
    Canceled = 5        // Manually canceled
}
