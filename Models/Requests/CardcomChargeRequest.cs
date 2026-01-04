namespace FunderPayments.Models.Requests;

public class CardcomChargeRequest
{
    public string TerminalNumber { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int APILevel { get; set; } = 10;
    public int Operation { get; set; } = 4; // Token charge
    public string Token { get; set; } = string.Empty;
    public decimal Sum { get; set; }
    public int CoinID { get; set; } = 1;
    public string Order { get; set; } = string.Empty;
    public string? APIPassword { get; set; }
    public string? CardOwnerId { get; set; }
    public Dictionary<string, string>? JParams { get; set; }
}
