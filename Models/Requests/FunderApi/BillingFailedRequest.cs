namespace FunderPayments.Models.Requests.FunderApi;

public class BillingFailedRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int CoinId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public int ResponseCode { get; set; }
    public string? Description { get; set; }
    public string? Error { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}

