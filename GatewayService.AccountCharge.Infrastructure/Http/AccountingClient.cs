// File: GatewayService.AccountCharge.Infrastructure/Http/AccountingClient.cs
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using GatewayService.AccountCharge.Application.Abstractions;

namespace GatewayService.AccountCharge.Infrastructure.Http
{
    public sealed class AccountingClient : IAccountingClient
    {
        private readonly HttpClient _http;
        public AccountingClient(HttpClient http) => _http = http;

        private sealed record DepositAppliedRequest(
            string? PaymentId,
            int? ExternalCustomerId,
            decimal Amount,
            string Currency,
            DateTimeOffset OccurredAtUtc,
            string? Gateway,
            string? Address,
            string? TxHash,
            string? CorrelationId
        );

        private sealed record OkResponse(string message, Guid? invoiceId);
        private sealed record ConflictResponse(string code, Guid? invoiceId, string message);

        public async Task<AccountingCreateDepositResult> CreateDepositAsync(
            Guid externalCustomerId,
            decimal amount,
            string currency,
            DateTimeOffset occurredAt,
            string? idempotencyKey,
            CancellationToken ct)
        {
            var body = new DepositAppliedRequest(
                PaymentId: idempotencyKey,
                ExternalCustomerId: null,                   // Party resolved by JWT on Accounting
                Amount: amount,
                Currency: currency,
                OccurredAtUtc: occurredAt,
                Gateway: "Nobitex",
                Address: null,
                TxHash: idempotencyKey,
                CorrelationId: externalCustomerId.ToString()
            );

            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v1/deposits/applied")
            {
                Content = JsonContent.Create(body)
            };
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
                req.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

            using var res = await _http.SendAsync(req, ct);

            if (res.StatusCode == HttpStatusCode.Conflict)
            {
                var c = await res.Content.ReadFromJsonAsync<ConflictResponse>(cancellationToken: ct);
                return new AccountingCreateDepositResult(false, true, c?.invoiceId, c?.message ?? "Duplicate transaction");
            }

            res.EnsureSuccessStatusCode();

            var ok = await res.Content.ReadFromJsonAsync<OkResponse>(cancellationToken: ct);
            return new AccountingCreateDepositResult(true, false, ok?.invoiceId, ok?.message);
        }

        // Back-compat adapter for older callers
        public async Task<ProcessDepositAppliedResult> ProcessDepositAppliedAsync(
            int externalCustomerId,
            string paymentId,
            decimal amount,
            string currency,
            DateTimeOffset occurredAtUtc,
            string gateway,
            string? address,
            string txHash,
            CancellationToken ct)
        {
            var body = new DepositAppliedRequest(
                PaymentId: paymentId,
                ExternalCustomerId: null,                   // prefer JWT party resolution
                Amount: amount,
                Currency: currency,
                OccurredAtUtc: occurredAtUtc,
                Gateway: gateway,
                Address: address,
                TxHash: txHash,
                CorrelationId: externalCustomerId.ToString()
            );

            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v1/deposits/applied")
            {
                Content = JsonContent.Create(body)
            };
            // Use txHash as idempotency key when available; fallback to paymentId
            var idem = string.IsNullOrWhiteSpace(txHash) ? paymentId : txHash;
            if (!string.IsNullOrWhiteSpace(idem))
                req.Headers.TryAddWithoutValidation("Idempotency-Key", idem);

            using var res = await _http.SendAsync(req, ct);

            if (res.StatusCode == HttpStatusCode.Conflict)
            {
                var c = await res.Content.ReadFromJsonAsync<ConflictResponse>(cancellationToken: ct);
                return new ProcessDepositAppliedResult(c?.invoiceId, true);
            }

            res.EnsureSuccessStatusCode();

            var ok = await res.Content.ReadFromJsonAsync<OkResponse>(cancellationToken: ct);
            return new ProcessDepositAppliedResult(ok?.invoiceId, false);
        }
    }
}
