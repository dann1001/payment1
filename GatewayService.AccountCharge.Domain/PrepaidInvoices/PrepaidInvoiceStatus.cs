namespace GatewayService.AccountCharge.Domain.PrepaidInvoices;

public enum PrepaidInvoiceStatus
{
    AwaitingConfirmations = 0,
    Paid = 1,
    RejectedWrongAddress = 2,
    RejectedCurrencyMismatch = 3,
    Expired = 4,
    AccountingSyncFailed = 5
}
