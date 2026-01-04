namespace FunderPayments.Models.Requests;

/// <summary>
/// Request model for Cardcom GetLpResult API - used to verify transaction data
/// </summary>
public class CardcomGetLpResultRequest
{
    public string TerminalNumber { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string LowProfileId { get; set; } = string.Empty;
    public string? ApiPassword { get; set; }
}

