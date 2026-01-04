namespace FunderPayments.Models.Requests;

public class ManualChargeRequest
{
    public string UserId { get; set; } = string.Empty;
    public Guid? TokenId { get; set; }
    public decimal Amount { get; set; }
    public int CoinId { get; set; } = 1;
}
