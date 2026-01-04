using System.Text.Json.Serialization;

namespace FunderPayments.Models.Requests;

/// <summary>
/// Request model for Cardcom JSON API v11 LowProfile/Create
/// </summary>
public class CardcomJsonTokenRequest
{
    [JsonPropertyName("TerminalNumber")]
    public string TerminalNumber { get; set; } = string.Empty;

    [JsonPropertyName("ApiName")]
    public string ApiName { get; set; } = string.Empty;

    [JsonPropertyName("Operation")]
    public string Operation { get; set; } = "ChargeAndCreateToken"; // "ChargeOnly", "ChargeAndCreateToken", "CreateTokenOnly", "SuspendedDeal"

    [JsonPropertyName("Amount")]
    public decimal Amount { get; set; } // Required - must be > 0

    [JsonPropertyName("ISOCoinId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ISOCoinId { get; set; }

    [JsonPropertyName("ReturnValue")]
    public string ReturnValue { get; set; } = string.Empty; // Required - order number or identifier

    [JsonPropertyName("SuccessRedirectUrl")]
    public string SuccessRedirectUrl { get; set; } = string.Empty;

    [JsonPropertyName("FailedRedirectUrl")]
    public string FailedRedirectUrl { get; set; } = string.Empty;

    [JsonPropertyName("WebHookUrl")]
    public string WebHookUrl { get; set; } = string.Empty;

    [JsonPropertyName("CustomFields")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CustomField>? CustomFields { get; set; }

    [JsonPropertyName("APIPassword")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? APIPassword { get; set; }
}

public class CustomField
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("Value")]
    public string Value { get; set; } = string.Empty;
}

