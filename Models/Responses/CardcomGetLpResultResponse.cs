namespace FunderPayments.Models.Responses;

/// <summary>
/// Verified response from Cardcom GetLpResult API
/// This is the authoritative source for transaction data
/// </summary>
public class CardcomGetLpResultResponse
{
    public int ResponseCode { get; set; }
    public string? Description { get; set; }
    public string? Token { get; set; }
    public string? ApproveNumber { get; set; }
    public string? DealResponse { get; set; }
    public string? CardType { get; set; }
    public string? L4digit { get; set; }
    public int? CardValidityMonth { get; set; }
    public int? CardValidityYear { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? ReturnValue { get; set; }
    public string? InternalDealNumber { get; set; }
    public string? ErrorText { get; set; }
    public string? Raw { get; set; }
}

