// File: D:\GatewayService.AccountCharge\GatewayService.AccountCharge.Infrastructure\Http\AccountingClient.cs
using System.Net.Http.Json;
using GatewayService.AccountCharge.Application.Abstractions;

namespace GatewayService.AccountCharge.Infrastructure.Http;

public sealed class AccountingClient(HttpClient http) : IAccountingClient
{
    private sealed record DepositAppliedRequest(
        string PaymentId,
        int? ExternalCustomerId, // optional; null => PartyResolver
        decimal Amount,
        string Currency,
        DateTimeOffset OccurredAtUtc,
        string? Gateway,
        string? Address,
        string? TxHash,
        string? CorrelationId
    );

    public async Task CreateDepositAsync(
        Guid externalCustomerId,          // ⚠️ هنوز امضای Interface اینجوریه؛ می‌تونیم این Guid رو به عنوان Correlation استفاده کنیم
        decimal amount,
        string currency,
        DateTimeOffset occurredAt,
        string? idempotencyKey,
        CancellationToken ct)
    {
        var body = new DepositAppliedRequest(
            PaymentId: idempotencyKey ?? $"dep-{externalCustomerId}-{occurredAt:yyyyMMddHHmmss}",
            ExternalCustomerId: null,                  // ✅ PartyResolver در Accounting از JWT به Party می‌رسد
            Amount: amount,
            Currency: currency,
            OccurredAtUtc: occurredAt,
            Gateway: "Nobitex",
            Address: null,                             // اگر داری پاس بده
            TxHash: idempotencyKey,                    // همون txhash
            CorrelationId: externalCustomerId.ToString()
        );

        using var req = new HttpRequestMessage(HttpMethod.Post, "api/v1/deposits/applied")
        {
            Content = JsonContent.Create(body)
        };

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            req.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        using var res = await http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }
}
