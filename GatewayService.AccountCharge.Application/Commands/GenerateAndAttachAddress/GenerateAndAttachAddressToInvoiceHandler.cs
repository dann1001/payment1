using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Application.DTOs;
using GatewayService.AccountCharge.Domain.Repositories;
using GatewayService.AccountCharge.Domain.ValueObjects;
using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.GenerateAndAttachAddress;

public sealed class GenerateAndAttachAddressToInvoiceHandler
    : IRequestHandler<GenerateAndAttachAddressToInvoiceCommand, GeneratedAddressResult>
{
    private readonly INobitexClient _nobitex;
    private readonly IInvoiceRepository _repo;
    private readonly IUnitOfWork _uow;

    public GenerateAndAttachAddressToInvoiceHandler(
        INobitexClient nobitex,
        IInvoiceRepository repo,
        IUnitOfWork uow)
    {
        _nobitex = nobitex;
        _repo = repo;
        _uow = uow;
    }

    public async Task<GeneratedAddressResult> Handle(GenerateAndAttachAddressToInvoiceCommand request, CancellationToken ct)
    {
        // 1) Load invoice (tracked)
        var invoice = await _repo.GetByIdAsync(request.InvoiceId, ct);
        if (invoice is null)
            throw new KeyNotFoundException("Invoice not found.");

        // 2) Upstream (Nobitex)
        var gen = await _nobitex.GenerateAddressAsync(request.Currency, request.Network, ct);

        // 3) Normalize
        static string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        var address = T(gen.Address);
        var tag = T(gen.Tag);
        var network = T(gen.Network) ?? T(request.Network);
        var currency = T(gen.Currency) ?? T(request.Currency);

        if (string.IsNullOrWhiteSpace(address))
            throw new InvalidOperationException("Nobitex did not return a deposit address.");
        if (string.IsNullOrWhiteSpace(network))
            throw new ArgumentException("Network is required.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.");

        // 4) Wallet id
        var walletId = gen.WalletId > 0
            ? gen.WalletId
            : await ResolveWalletIdByCurrencyAsync(_nobitex, currency!, ct);

        // 5) Map & attach
        var chainAddress = new ChainAddress(address!, network!, tag);
        var walletRef = new WalletRef(walletId, currency!);
        var now = (gen.CreatedAt == default) ? DateTimeOffset.UtcNow : gen.CreatedAt;

        invoice.AddAddress(chainAddress, walletRef, now);

        // 6) Persist
        await _uow.SaveChangesAsync(ct);

        // 7) Return DTO
        return new GeneratedAddressResult(
            Address: address!,
            Tag: tag,
            Network: network!,
            WalletId: walletId,
            Currency: currency!,
            CreatedAt: now
        );
    }

    private static async Task<int> ResolveWalletIdByCurrencyAsync(INobitexClient nobitex, string currency, CancellationToken ct)
    {
        var wallets = await nobitex.GetWalletsAsync(ct);
        var w = wallets.FirstOrDefault(x => string.Equals(x.Currency, currency, StringComparison.OrdinalIgnoreCase));
        if (w is null || w.Id <= 0)
            throw new InvalidOperationException($"No wallet found for currency '{currency}'.");
        return w.Id;
    }
}
