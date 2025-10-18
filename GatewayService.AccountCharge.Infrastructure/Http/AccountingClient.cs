// D:\GatewayService.AccountCharge\GatewayService.AccountCharge.Infrastructure\Http\AccountingClient.cs
using System.Net.Http.Json;
using GatewayService.AccountCharge.Application.Abstractions;

namespace GatewayService.AccountCharge.Infrastructure.Http;

public sealed class AccountingClient(HttpClient http) : IAccountingClient
{
    private sealed record CreateAccountingInvoiceRequest(
        Guid ExternalCustomerId, int Tag, decimal Amount, string Currency, DateTimeOffset OccurredAt);

    public async Task CreateDepositAsync(
        Guid externalCustomerId,
        decimal amount,
        string currency,
        DateTimeOffset occurredAt,
        string? idempotencyKey,
        CancellationToken ct)
    {
        var body = new CreateAccountingInvoiceRequest(
            ExternalCustomerId: externalCustomerId,
            Tag: 1,                       // deposit
            Amount: amount,
            Currency: currency,
            OccurredAt: occurredAt
        );

        using var req = new HttpRequestMessage(HttpMethod.Post, "api/v1/invoices")
        {
            Content = JsonContent.Create(body)
        };

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            req.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        using var res = await http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }
}
