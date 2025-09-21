using System.Text.Json.Serialization;

namespace GatewayService.AccountCharge.Infrastructure.Http.Models;

internal sealed class NobitexDepositsResponse
{
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("deposits")] public List<NobitexDeposit>? Deposits { get; set; }
    [JsonPropertyName("hasNext")] public bool? HasNext { get; set; }
}

internal sealed class NobitexDeposit
{
    [JsonPropertyName("txHash")] public string? TxHash { get; set; }
    [JsonPropertyName("address")] public string? Address { get; set; }
    [JsonPropertyName("confirmed")] public bool Confirmed { get; set; }

    [JsonPropertyName("transaction")] public NobitexTransaction? Transaction { get; set; }

    [JsonPropertyName("currency")] public string? CurrencyName { get; set; } // e.g. "Bitcoin"
    [JsonPropertyName("blockchainUrl")] public string? BlockchainUrl { get; set; }
    [JsonPropertyName("confirmations")] public int Confirmations { get; set; }
    [JsonPropertyName("requiredConfirmations")] public int RequiredConfirmations { get; set; }
    [JsonPropertyName("amount")] public string? Amount { get; set; }
}

internal sealed class NobitexTransaction
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("amount")] public string? Amount { get; set; }
    [JsonPropertyName("currency")] public string? Currency { get; set; } // e.g. "btc"
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("calculatedFee")] public string? CalculatedFee { get; set; }
}
