namespace GatewayService.AccountCharge.Application.Interfaces
{
    public interface IAddressGenerator
    {
        Task<(string Address, Guid WalletId)> GenerateAsync(
            string currency,
            string? network,
            CancellationToken cancellationToken);
    }
}
