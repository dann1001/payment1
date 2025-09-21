using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Domain.Invoices;
using GatewayService.AccountCharge.Domain.Repositories;
using GatewayService.AccountCharge.Domain.ValueObjects;
using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.ApplyDeposit;

public sealed class ApplyDepositHandler : IRequestHandler<ApplyDepositCommand, ApplyDepositResult>
{
    private readonly IInvoiceRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IPaymentMatchingOptionsProvider _optsProvider;

    public ApplyDepositHandler(IInvoiceRepository repo, IUnitOfWork uow, IPaymentMatchingOptionsProvider optsProvider)
    {
        _repo = repo;
        _uow = uow;
        _optsProvider = optsProvider;
    }

    public async Task<ApplyDepositResult> Handle(ApplyDepositCommand request, CancellationToken ct)
    {
        // Resolve invoice by address+network
        var chainAddress = new ChainAddress(request.Address, request.Network, request.Tag);
        var invoice = await _repo.GetByAddressAsync(chainAddress, ct);

        if (invoice is null)
            return new ApplyDepositResult(false, false, "No invoice owns this address", null);

        var incoming = new IncomingDeposit(
            new TransactionHash(request.TxHash),
            chainAddress,
            new Money(request.Amount, request.Currency),
            request.Confirmed,
            request.Confirmations,
            request.RequiredConfirmations,
            request.CreatedAt
        );

        var opts = _optsProvider.Get(request.Currency, request.Network);
        var ok = invoice.TryApplyDeposit(incoming, opts, out var reason);

        if (ok)
        {
            await _repo.UpdateAsync(invoice, ct);
            await _uow.SaveChangesAsync(ct);
            // If idempotent re-apply, TryApplyDeposit returns true with reason="Already applied"
            return new ApplyDepositResult(true, reason != "Already applied", reason, invoice.Id);
        }

        return new ApplyDepositResult(true, false, reason, invoice.Id);
    }
}
