using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Domain.PrepaidInvoices;
using GatewayService.AccountCharge.Domain.Repositories;
using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.Prepaid;

public sealed class CreatePrepaidInvoiceHandler : IRequestHandler<CreatePrepaidInvoiceCommand, Guid>
{
    private readonly IPrepaidInvoiceRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreatePrepaidInvoiceHandler(IPrepaidInvoiceRepository repo, IUnitOfWork uow)
    {
        _repo = repo; _uow = uow;
    }

    public async Task<Guid> Handle(CreatePrepaidInvoiceCommand request, CancellationToken ct)
    {
        // idempotency by txHash
        var existing = await _repo.GetByTxHashAsync(request.TxHash.Trim(), ct);
        if (existing is not null) return existing.Id;

        var entity = PrepaidInvoice.Create(request.Currency, request.Network, request.TxHash, request.CustomerId, request.Ttl);
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return entity.Id;
    }
}
