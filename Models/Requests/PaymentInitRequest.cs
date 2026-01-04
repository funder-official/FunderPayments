namespace FunderPayments.Models.Requests;

public class PaymentInitRequest
{
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int? CoinId { get; set; }
    public string? SuccessRedirectUrl { get; set; }
    public string? ErrorRedirectUrl { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
