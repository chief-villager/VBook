using System.Text.Json.Serialization;

namespace Bookkeeping.Infrastructure.Mono;

public sealed class MonoAccountResponse
{
    [JsonPropertyName("data")]
    public MonoAccountData? Data { get; set; }
}

public sealed class MonoAccountData
{
    [JsonPropertyName("account")]
    public MonoAccount? Account { get; set; }
}

public sealed class MonoAccount
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("account_number")]
    public string? AccountNumber { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("balance")]
    public long Balance { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("bvn")]
    public string? Bvn { get; set; }

    [JsonPropertyName("institution")]
    public MonoInstitution? Institution { get; set; }
}

public sealed class MonoInstitution
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("bank_code")]
    public string? BankCode { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public sealed class MonoUnlinkResponse
{   
    [JsonPropertyName("status")]
    public string? Status {get; set;}

    [JsonPropertyName("message")]
    public string? Message {get; set;}
    
    [JsonPropertyName ("timestamp")]
    public DateTime Timestamp {get; set;}
}

public sealed class MonoTransaction
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }        // minor units (kobo)

    [JsonPropertyName("narration")]
    public string? Narration { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }       // "credit" | "debit"

    [JsonPropertyName("balance")]
    public long Balance { get; set; }

    [JsonPropertyName("date")]
    public DateTimeOffset Date { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }
}

