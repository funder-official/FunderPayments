namespace FunderPayments.Models.Responses;

public class PaymentInitResult
{
    public string PaymentPageUrl { get; set; } = string.Empty;
    public string? IframeHtml { get; set; }
    public Dictionary<string, object> Payload { get; set; } = new();
}
