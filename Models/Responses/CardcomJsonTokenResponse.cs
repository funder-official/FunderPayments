using System.Text.Json.Serialization;

namespace FunderPayments.Models.Responses;

/// <summary>
/// Response model for Cardcom JSON API v11 LowProfile/Create
/// </summary>
public class CardcomJsonTokenResponse
{
    [JsonPropertyName("ResponseCode")]
    public int ResponseCode { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("LowProfileId")]
    public string? LowProfileId { get; set; }

    [JsonPropertyName("Url")]
    public string? Url { get; set; }
}

