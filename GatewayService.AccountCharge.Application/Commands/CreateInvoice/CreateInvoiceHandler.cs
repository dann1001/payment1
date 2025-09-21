using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Domain.Invoices;
using GatewayService.AccountCharge.Domain.Repositories;
using GatewayService.AccountCharge.Domain.ValueObjects;
using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.CreateInvoice;

public sealed class CreateInvoiceHandler : IRequestHandler<CreateInvoiceCommand, Guid>
{
    private readonly IInvoiceRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IInvoiceNumberGenerator _numberGen;

    public CreateInvoiceHandler(IInvoiceRepository repo, IUnitOfWork uow, IInvoiceNumberGenerator numberGen)
    {
        _repo = repo;
        _uow = uow;
        _numberGen = numberGen;
    }

    public async Task<Guid> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
        var number = string.IsNullOrWhiteSpace(request.InvoiceNumber)
            ? _numberGen.Next()
            : request.InvoiceNumber!.Trim();

        var expected = new Money(request.Amount, request.Currency);
        var invoice = Invoice.Create(number, expected, request.CustomerId, request.Ttl);

        await _repo.AddAsync(invoice, ct);
        await _uow.SaveChangesAsync(ct);

        return invoice.Id;
    }
}
