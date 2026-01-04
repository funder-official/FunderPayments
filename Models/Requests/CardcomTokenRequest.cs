using System.Text.Json.Serialization;

namespace FunderPayments.Models.Requests;

public class CardcomTokenRequest
{
    public string TerminalNumber { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int APILevel { get; set; } = 10;
    public int Operation { get; set; } = 1; // Create transaction
    public int Tokenize { get; set; } = 1;
    public decimal? Sum { get; set; }
    public int? CoinID { get; set; }
    public string ReturnValue { get; set; } = string.Empty;
    public string SuccessRedirectUrl { get; set; } = string.Empty;
    public string ErrorRedirectUrl { get; set; } = string.Empty;
    public int? ValidateType { get; set; }
    public int? CreditType { get; set; }
    public bool? IsMustEnable { get; set; }
    public decimal? FirstPayment { get; set; }
    public decimal? ConstPayment { get; set; }
    public Dictionary<string, string>? JParams { get; set; }
    public string? APIPassword { get; set; }
}
