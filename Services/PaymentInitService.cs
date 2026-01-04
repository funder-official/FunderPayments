using FunderPayments.Models.Options;
using FunderPayments.Models.Requests;
using FunderPayments.Models.Responses;
using Microsoft.Extensions.Options;

namespace FunderPayments.Services;

public class PaymentInitService
{
    private readonly CardcomClient _client;
    private readonly CardcomOptions _options;
    private readonly ILogger<PaymentInitService> _logger;

    public PaymentInitService(CardcomClient client, IOptions<CardcomOptions> options, ILogger<PaymentInitService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PaymentInitResult> CreatePaymentPageAsync(PaymentInitRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("Amount must be greater than zero for payment initialization.");
        }

        var cardcomRequest = BuildCardcomRequest(request);
        var rawResponse = await _client.CreatePaymentPageAsync(cardcomRequest, cancellationToken);

        // Cardcom typically returns a URL to redirect/iframe. Preserve raw response for the caller.
        var iframe = $"<iframe src=\"{rawResponse}\" width=\"100%\" height=\"600\" frameborder=\"0\"></iframe>";

        return new PaymentInitResult
        {
            PaymentPageUrl = rawResponse,
            IframeHtml = iframe,
            Payload = BuildPayload(cardcomRequest)
        };
    }

    private CardcomJsonTokenRequest BuildCardcomRequest(PaymentInitRequest request)
    {
        // Build CustomFields from JParams (UserId and metadata)
        var customFields = new List<CustomField>();
        int fieldId = 1;
        
        // Add UserId as first custom field
        customFields.Add(new CustomField
        {
            Id = fieldId++,
            Value = request.UserId
        });

        // Add metadata as additional custom fields
        if (request.Metadata is not null)
        {
            foreach (var kv in request.Metadata)
            {
                if (fieldId > 25) break; // Max 25 custom fields
                customFields.Add(new CustomField
                {
                    Id = fieldId++,
                    Value = kv.Value.Length > 50 ? kv.Value.Substring(0, 50) : kv.Value // Max 50 chars
                });
            }
        }

        // Generate OrderId for ReturnValue (will be returned in webhook)
        // Format: ORDER-{UserId}-{MonthlyAmount}-{Timestamp}
        // This allows us to extract userId and monthlyAmount in the callback
        var orderId = $"ORDER-{request.UserId}-{request.Amount:F2}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var cardcomRequest = new CardcomJsonTokenRequest
        {
            TerminalNumber = _options.TerminalNumber,
            ApiName = _options.UserName, // JSON API uses ApiName (not UserName)
            Operation = "CreateTokenOnly", // Create token only (no initial charge)
            Amount = request.Amount, // Required - must be > 0
            ISOCoinId = request.CoinId ?? _options.DefaultCoinId,
            ReturnValue = orderId, // Order identifier (returned in webhook)
            SuccessRedirectUrl = request.SuccessRedirectUrl ?? _options.SuccessRedirectUrl,
            FailedRedirectUrl = request.ErrorRedirectUrl ?? _options.ErrorRedirectUrl,
            WebHookUrl = _options.CallbackUrl, // Required - callback URL for webhook
            CustomFields = customFields.Count > 0 ? customFields : null,
            APIPassword = _options.ApiPassword // Optional - for additional authentication
        };

        return cardcomRequest;
    }

    private Dictionary<string, object> BuildPayload(CardcomJsonTokenRequest request)
    {
        var payload = new Dictionary<string, object>
        {
            ["TerminalNumber"] = request.TerminalNumber,
            ["ApiName"] = request.ApiName,
            ["Operation"] = request.Operation,
            ["Amount"] = request.Amount,
            ["ReturnValue"] = request.ReturnValue,
            ["SuccessRedirectUrl"] = request.SuccessRedirectUrl,
            ["FailedRedirectUrl"] = request.FailedRedirectUrl,
            ["WebHookUrl"] = request.WebHookUrl
        };

        if (request.ISOCoinId.HasValue)
        {
            payload["ISOCoinId"] = request.ISOCoinId.Value;
        }

        if (request.CustomFields is not null && request.CustomFields.Count > 0)
        {
            payload["CustomFields"] = request.CustomFields;
        }

        return payload;
    }
}
