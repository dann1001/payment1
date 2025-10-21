// File: GatewayService.AccountCharge.Application/Abstractions/IAccountingClient.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayService.AccountCharge.Application.Abstractions
{
    // New-style result used by CreateDepositAsync (handles 409 Duplicate)
    public sealed record AccountingCreateDepositResult(bool Success, bool Duplicate, Guid? InvoiceId, string? Message);

    // Legacy-style result (kept for backward compatibility)
    public sealed record ProcessDepositAppliedResult(Guid? InvoiceId, bool Duplicate);

    public interface IAccountingClient
    {
        // ✅ New unified call used by Prepaid/Apply flows (USDT after conversion)
        Task<AccountingCreateDepositResult> CreateDepositAsync(
            Guid externalCustomerId,
            decimal amount,
            string currency,
            DateTimeOffset occurredAt,
            string? idempotencyKey,
            CancellationToken ct);

        // ✅ Legacy signature kept so existing callers won’t break (adapts to the same endpoint internally)
        Task<ProcessDepositAppliedResult> ProcessDepositAppliedAsync(
            int externalCustomerId,
            string paymentId,
            decimal amount,
            string currency,
            DateTimeOffset occurredAtUtc,
            string gateway,
            string? address,
            string txHash,
            CancellationToken ct);
    }
}
