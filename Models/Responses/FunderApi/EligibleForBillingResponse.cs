namespace FunderPayments.Models.Responses.FunderApi;

public class EligibleForBillingResponse
{
    public List<EligibleUser> EligibleUsers { get; set; } = new();
}

public class EligibleUser
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public decimal MonthlyAmount { get; set; }
    public bool IsEligible { get; set; }
    public string? Reason { get; set; } // Optional reason if not eligible
}

