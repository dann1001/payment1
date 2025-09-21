using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Domain.Repositories;
using GatewayService.AccountCharge.Domain.ValueObjects;
using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.AttachAddress;

public sealed class AttachAddressToInvoiceHandler : IRequestHandler<AttachAddressToInvoiceCommand, bool>
{
    private readonly IInvoiceRepository _repo;
    private readonly IUnitOfWork _uow;

    public AttachAddressToInvoiceHandler(IInvoiceRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<bool> Handle(AttachAddressToInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await _repo.GetByIdAsync(request.InvoiceId, ct);
        if (invoice is null) return false;

        var chainAddress = new ChainAddress(request.Address, request.Network, request.Tag);
        var walletRef = new WalletRef(request.WalletId, request.Currency);

        invoice.AddAddress(chainAddress, walletRef);

        await _repo.UpdateAsync(invoice, ct);
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}
