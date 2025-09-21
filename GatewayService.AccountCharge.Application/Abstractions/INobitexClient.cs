using GatewayService.AccountCharge.Application.DTOs;

namespace GatewayService.AccountCharge.Application.Abstractions;

public interface INobitexClient
{
    Task<IReadOnlyList<WalletDto>> GetWalletsAsync(CancellationToken ct);

    // Preferred: currency required, network optional
    Task<GeneratedAddressDto> GenerateAddressAsync(string currency, string? network, CancellationToken ct);

    // walletId is INT (Nobitex returns numeric id)
    Task<IReadOnlyList<IncomingDepositDto>> GetRecentDepositsAsync(int walletId, int limit, DateTimeOffset? since, CancellationToken ct);
}
