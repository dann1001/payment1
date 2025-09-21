namespace GatewayService.AccountCharge.Application.Abstractions;

/// <summary>
/// Generates unique, user-facing invoice numbers.
/// </summary>
public interface IInvoiceNumberGenerator
{
    string Next();
}
