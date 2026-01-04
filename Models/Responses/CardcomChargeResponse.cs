namespace FunderPayments.Models.Responses;

public class CardcomChargeResponse
{
    public int ResponseCode { get; set; }
    public string? Description { get; set; }
    public string? ApproveNumber { get; set; }
    public string? InternalDealNumber { get; set; }
    public string? DealResponse { get; set; }
    public string? Token { get; set; }
    public string Raw { get; set; } = string.Empty;
}
