using FunderPayments.Models.Requests;
using FunderPayments.Models.Responses;
using FunderPayments.Services;
using Microsoft.AspNetCore.Mvc;

namespace FunderPayments.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentInitService _paymentInitService;
    private readonly CallbackService _callbackService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(PaymentInitService paymentInitService, CallbackService callbackService, ILogger<PaymentsController> logger)
    {
        _paymentInitService = paymentInitService;
        _callbackService = callbackService;
        _logger = logger;
    }

    [HttpPost("init")]
    public async Task<ActionResult<PaymentInitResult>> Init([FromBody] PaymentInitRequest request, CancellationToken cancellationToken)
    {
        var result = await _paymentInitService.CreatePaymentPageAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("callback")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Callback([FromForm] CardcomTokenResponse callback, CancellationToken cancellationToken)
    {
        var form = Request.HasFormContentType ? Request.Form : null;
        var (userId, monthlyAmount) = ExtractUserIdAndAmount(form);

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Callback received without userId. ReturnValue: {ReturnValue}", 
                form?.TryGetValue("ReturnValue", out var rv) == true ? rv.ToString() : "N/A");
            return Ok(); // Return 200 to Cardcom even if we can't process
        }

        await _callbackService.ProcessCallbackAsync(callback, userId, monthlyAmount, form, cancellationToken);
        return Ok(); // Always return 200 to Cardcom
    }

    private static (string userId, decimal? monthlyAmount) ExtractUserIdAndAmount(IFormCollection? form)
    {
        if (form is null) return (string.Empty, null);

        // Try to extract from ReturnValue (format: ORDER-{UserId}-{MonthlyAmount}-{Timestamp})
        if (form.TryGetValue("ReturnValue", out var returnValue))
        {
            var rv = returnValue.ToString();
            if (rv.StartsWith("ORDER-", StringComparison.OrdinalIgnoreCase))
            {
                var parts = rv.Split('-');
                if (parts.Length >= 3)
                {
                    var userId = parts[1];
                    if (decimal.TryParse(parts[2], out var amount))
                    {
                        return (userId, amount);
                    }
                    // Fallback: if amount parsing fails, still return userId
                    return (userId, null);
                }
                else if (parts.Length >= 2)
                {
                    // Old format: ORDER-{UserId}-{Timestamp}
                    return (parts[1], null);
                }
            }
        }

        // Fallback: Try JParams[UserId] (if CustomFields are returned as JParams)
        if (form.TryGetValue("JParams[UserId]", out var fromJParams))
        {
            return (fromJParams.ToString(), null);
        }

        // Fallback: Try direct UserId
        if (form.TryGetValue("UserId", out var direct))
        {
            return (direct.ToString(), null);
        }

        // Try CustomFields format (if Cardcom returns them)
        for (int i = 1; i <= 25; i++)
        {
            if (form.TryGetValue($"CustomFields[{i}]", out var customField))
            {
                var value = customField.ToString();
                // If first custom field and looks like a userId, use it
                if (i == 1 && !string.IsNullOrWhiteSpace(value))
                {
                    return (value, null);
                }
            }
        }

        return (string.Empty, null);
    }
}
