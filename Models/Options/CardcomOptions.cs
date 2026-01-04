namespace FunderPayments.Models.Options;

public class CardcomOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string PaymentPagePath { get; set; } = "api/v11/Transactions/PaymentPage";
    public string DoTransactionPath { get; set; } = "api/v11/Transactions/Transaction";
    public string GetLpResultPath { get; set; } = "api/v11/LowProfile/GetLpResult";
    public string TerminalNumber { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? ApiPassword { get; set; }
    public int ApiLevel { get; set; } = 10;
    public int TimeoutSeconds { get; set; } = 30;
    public string SuccessRedirectUrl { get; set; } = string.Empty;
    public string ErrorRedirectUrl { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public int DefaultCoinId { get; set; } = 1;
    public decimal DefaultMonthlyAmount { get; set; } = 0m;
}
