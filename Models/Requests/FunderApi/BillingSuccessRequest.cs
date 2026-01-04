namespace FunderPayments.Models.Requests.FunderApi;

public class BillingSuccessRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int CoinId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string ApproveNumber { get; set; } = string.Empty;
    public string? InternalDealNumber { get; set; }
    public DateTime ChargedAt { get; set; } = DateTime.UtcNow;
}

