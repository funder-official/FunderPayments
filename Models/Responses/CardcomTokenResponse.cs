using Microsoft.AspNetCore.Mvc;

namespace FunderPayments.Models.Responses;

public class CardcomTokenResponse
{
    [FromForm(Name = "ResponseCode")]
    public int ResponseCode { get; set; }

    [FromForm(Name = "Token")]
    public string? Token { get; set; }

    [FromForm(Name = "DealResponse")]
    public string? DealResponse { get; set; }

    [FromForm(Name = "ApproveNumber")]
    public string? ApproveNumber { get; set; }

    [FromForm(Name = "CardType")]
    public string? CardType { get; set; }

    [FromForm(Name = "L4digit")]
    public string? L4digit { get; set; }

    [FromForm(Name = "CardValidityMonth")]
    public int? CardValidityMonth { get; set; }

    [FromForm(Name = "CardValidityYear")]
    public int? CardValidityYear { get; set; }

    [FromForm(Name = "InvoiceNumber")]
    public string? InvoiceNumber { get; set; }

    [FromForm(Name = "ErrorText")]
    public string? ErrorText { get; set; }

    [FromForm(Name = "ReturnValue")]
    public string? ReturnValue { get; set; }

    [FromForm(Name = "LowProfileId")]
    public string? LowProfileId { get; set; }

    [FromForm]
    public Dictionary<string, string>? FormData { get; set; }
}
