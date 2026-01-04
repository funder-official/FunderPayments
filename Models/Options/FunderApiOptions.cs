namespace FunderPayments.Models.Options;

public class FunderApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    
    // Endpoints
    public string PaymentTokenRegisteredPath { get; set; } = "api/payments/token-registered";
    public string EligibleForBillingPath { get; set; } = "api/payments/eligible-for-billing";
    public string BillingSuccessPath { get; set; } = "api/payments/billing/success";
    public string BillingFailedPath { get; set; } = "api/payments/billing/failed";
}

