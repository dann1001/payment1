using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.ApplyDeposit;

public sealed record ApplyDepositToInvoiceCommand(
    Guid InvoiceId,
    string TxHash,
    string Address,
    string? Network,
    string? Tag,
    decimal Amount,
    string Currency,
    bool Confirmed,
    int Confirmations,
    int RequiredConfirmations,
    DateTimeOffset CreatedAt
) : IRequest<ApplyDepositResult>;
/// <summary>Result of applying a deposit to an invoice.</summary>
public sealed record ApplyDepositResult(
    bool Matched,
    bool Applied,
    string Reason,
    Guid? InvoiceId
);

public sealed class DepositForInvoiceDto
{
    public Guid InvoiceId { get; init; }   // 💡 اضافه شده
    public string TxHash { get; init; } = default!;
    public string Address { get; init; } = default!;
    public string? Network { get; init; }
    public string? Tag { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = default!;
    public bool Confirmed { get; init; }
    public int Confirmations { get; init; }
    public int RequiredConfirmations { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}