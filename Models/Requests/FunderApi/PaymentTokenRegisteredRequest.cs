namespace FunderPayments.Models.Requests.FunderApi;

public class PaymentTokenRegisteredRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string? CardType { get; set; }
    public string? Last4Digits { get; set; }
    public decimal MonthlyAmount { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

