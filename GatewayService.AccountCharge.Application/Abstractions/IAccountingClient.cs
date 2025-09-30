// D:\GatewayService.AccountCharge\GatewayService.AccountCharge.Application\Abstractions\IAccountingClient.cs
using System;

namespace GatewayService.AccountCharge.Application.Abstractions;

public interface IAccountingClient
{
    /// <summary>Send a Deposit invoice to Accounting (tag = 1).</summary>
    Task CreateDepositAsync(
        Guid externalCustomerId,
        decimal amount,
        string currency,
        DateTimeOffset occurredAt,
        string? idempotencyKey,
        CancellationToken ct);
}
