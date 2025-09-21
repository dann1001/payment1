using GatewayService.AccountCharge.Domain.Invoices;

namespace GatewayService.AccountCharge.Application.Abstractions;


/// <summary>
/// Provides matching options (confirmations/tolerance) per currency/network or globally.
/// </summary>
public interface IPaymentMatchingOptionsProvider
{
    PaymentMatchingOptions Get(string currency, string network);
}
