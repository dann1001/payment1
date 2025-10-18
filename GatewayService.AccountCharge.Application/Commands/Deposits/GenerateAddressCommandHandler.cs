// File: Application/Commands/Deposits/GenerateAddressCommandHandler.cs
using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Application.Contracts.Deposits;
using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.Deposits;

internal sealed class GenerateAddressCommandHandler
    : IRequestHandler<GenerateAddressCommand, GeneratedAddressResult>
{
    private readonly INobitexClient _nobitex;

    public GenerateAddressCommandHandler(INobitexClient nobitex)
    {
        _nobitex = nobitex;
    }

    public async Task<GeneratedAddressResult> Handle(
        GenerateAddressCommand request,
        CancellationToken cancellationToken)
    {
        var dto = await _nobitex.GenerateAddressAsync(
            request.Currency,
            request.Network,
            cancellationToken);

        return new GeneratedAddressResult
        {
            Address = dto.Address,
            Currency = dto.Currency,
            Network = dto.Network,
            WalletId = dto.WalletId,
            CreatedAt = dto.CreatedAt
        };
    }
}
