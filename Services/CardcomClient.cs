using FunderPayments.Models.Options;
using FunderPayments.Models.Requests;
using FunderPayments.Models.Responses;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FunderPayments.Services;

public class CardcomClient
{
    private readonly HttpClient _httpClient;
    private readonly CardcomOptions _options;
    private readonly ILogger<CardcomClient> _logger;

    public CardcomClient(HttpClient httpClient, IOptions<CardcomOptions> options, ILogger<CardcomClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> CreatePaymentPageAsync(CardcomJsonTokenRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending PaymentPage JSON request to Cardcom. BaseUrl: {BaseUrl}, Path: {Path}",
            _httpClient.BaseAddress?.ToString(), _options.PaymentPagePath);

        // Cardcom expects PascalCase property names (TerminalNumber, ApiName, etc.)
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = System.Text.Json.JsonSerializer.Serialize(request, jsonOptions);

        // Build full URL
        var requestUri = _httpClient.BaseAddress != null 
            ? new Uri(_httpClient.BaseAddress, _options.PaymentPagePath)
            : new Uri(new Uri(_options.BaseUrl.TrimEnd('/') + "/"), _options.PaymentPagePath);
        
        _logger.LogDebug("Full request URI: {Uri}", requestUri);
        _logger.LogDebug("Request JSON: {Json}", json);

        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogDebug("PaymentPage raw JSON response: {Body}", body);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PaymentPage request failed. Status: {Status}, Body: {Body}", response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        // Parse JSON response
        var jsonResponse = System.Text.Json.JsonSerializer.Deserialize<CardcomJsonTokenResponse>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (jsonResponse?.ResponseCode != 0 || string.IsNullOrWhiteSpace(jsonResponse.Url))
        {
            _logger.LogError("Cardcom CreatePaymentPage failed. ResponseCode: {Code}, Description: {Desc}", 
                jsonResponse?.ResponseCode, jsonResponse?.Description);
            throw new HttpRequestException($"Cardcom CreatePaymentPage failed: {jsonResponse?.Description ?? "Unknown error"}");
        }

        return jsonResponse.Url;
    }

    /// <summary>
    /// CRITICAL: Verify transaction data by calling GetLpResult
    /// This must be called after receiving webhook to verify the transaction
    /// </summary>
    public async Task<CardcomGetLpResultResponse> GetLpResultAsync(CardcomGetLpResultRequest request, CancellationToken cancellationToken = default)
    {
        var payload = ToFormUrlEncoded(request);
        _logger.LogInformation("Verifying transaction data via GetLpResult for LowProfileId {LowProfileId}", request.LowProfileId);

        using var content = new FormUrlEncodedContent(payload);
        
        // Build full URL
        var requestUri = _httpClient.BaseAddress != null 
            ? new Uri(_httpClient.BaseAddress, _options.GetLpResultPath)
            : new Uri(new Uri(_options.BaseUrl.TrimEnd('/') + "/"), _options.GetLpResultPath);
        
        _logger.LogDebug("GetLpResult request URI: {Uri}", requestUri);
        
        var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogDebug("GetLpResult raw response: {Body}", body);
        response.EnsureSuccessStatusCode();

        return MapGetLpResultResponse(body);
    }

    public async Task<CardcomChargeResponse> ChargeTokenAsync(CardcomChargeRequest request, CancellationToken cancellationToken = default)
    {
        var payload = ToFormUrlEncoded(request);
        _logger.LogInformation("Sending Do-Transaction request for order {Order}", request.Order);

        using var content = new FormUrlEncodedContent(payload);
        
        // Build full URL if BaseAddress is not set
        var requestUri = _httpClient.BaseAddress != null 
            ? new Uri(_httpClient.BaseAddress, _options.DoTransactionPath)
            : new Uri(new Uri(_options.BaseUrl.TrimEnd('/') + "/"), _options.DoTransactionPath);
        
        _logger.LogDebug("Full request URI: {Uri}", requestUri);
        
        var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogDebug("Do-Transaction raw response: {Body}", body);
        response.EnsureSuccessStatusCode();

        return MapChargeResponse(body);
    }

    private static IEnumerable<KeyValuePair<string, string>> ToFormUrlEncoded(CardcomTokenRequest request)
    {
        var dict = new Dictionary<string, string>
        {
            ["TerminalNumber"] = request.TerminalNumber,
            ["UserName"] = request.UserName,
            ["APILevel"] = request.APILevel.ToString(),
            ["Operation"] = request.Operation.ToString(),
            ["Tokenize"] = request.Tokenize.ToString(),
            ["ReturnValue"] = request.ReturnValue,
            ["SuccessRedirectUrl"] = request.SuccessRedirectUrl,
            ["ErrorRedirectUrl"] = request.ErrorRedirectUrl
        };

        if (request.Sum.HasValue) dict["Sum"] = request.Sum.Value.ToString("0.##");
        if (request.CoinID.HasValue) dict["CoinID"] = request.CoinID.Value.ToString();
        if (request.ValidateType.HasValue) dict["ValidateType"] = request.ValidateType.Value.ToString();
        if (request.CreditType.HasValue) dict["CreditType"] = request.CreditType.Value.ToString();
        if (request.IsMustEnable.HasValue) dict["IsMustEnable"] = request.IsMustEnable.Value ? "1" : "0";
        if (request.FirstPayment.HasValue) dict["FirstPayment"] = request.FirstPayment.Value.ToString("0.##");
        if (request.ConstPayment.HasValue) dict["ConstPayment"] = request.ConstPayment.Value.ToString("0.##");
        if (!string.IsNullOrWhiteSpace(request.APIPassword)) dict["APIPassword"] = request.APIPassword!;

        if (request.JParams is not null)
        {
            foreach (var kv in request.JParams)
            {
                dict[$"JParams[{kv.Key}]"] = kv.Value;
            }
        }

        return dict;
    }

    private static IEnumerable<KeyValuePair<string, string>> ToFormUrlEncoded(CardcomChargeRequest request)
    {
        var dict = new Dictionary<string, string>
        {
            ["TerminalNumber"] = request.TerminalNumber,
            ["UserName"] = request.UserName,
            ["APILevel"] = request.APILevel.ToString(),
            ["Operation"] = request.Operation.ToString(),
            ["Token"] = request.Token,
            ["Sum"] = request.Sum.ToString("0.##"),
            ["CoinID"] = request.CoinID.ToString(),
            ["Order"] = request.Order
        };

        if (!string.IsNullOrWhiteSpace(request.APIPassword)) dict["APIPassword"] = request.APIPassword!;
        if (!string.IsNullOrWhiteSpace(request.CardOwnerId)) dict["CardOwnerId"] = request.CardOwnerId!;

        if (request.JParams is not null)
        {
            foreach (var kv in request.JParams)
            {
                dict[$"JParams[{kv.Key}]"] = kv.Value;
            }
        }

        return dict;
    }

    private static CardcomChargeResponse MapChargeResponse(string raw)
    {
        var dict = ParseQueryString(raw);
        var response = new CardcomChargeResponse
        {
            Raw = raw
        };

        if (dict.TryGetValue("ResponseCode", out var code) && int.TryParse(code, out var parsedCode))
        {
            response.ResponseCode = parsedCode;
        }

        dict.TryGetValue("Description", out var description);
        dict.TryGetValue("ApproveNumber", out var approve);
        dict.TryGetValue("InternalDealNumber", out var internalDeal);
        dict.TryGetValue("DealResponse", out var dealResponse);
        dict.TryGetValue("Token", out var token);

        response.Description = description;
        response.ApproveNumber = approve;
        response.InternalDealNumber = internalDeal;
        response.DealResponse = dealResponse;
        response.Token = token;

        return response;
    }

    private static IEnumerable<KeyValuePair<string, string>> ToFormUrlEncoded(CardcomGetLpResultRequest request)
    {
        var dict = new Dictionary<string, string>
        {
            ["TerminalNumber"] = request.TerminalNumber,
            ["UserName"] = request.UserName,
            ["LowProfileId"] = request.LowProfileId
        };

        if (!string.IsNullOrWhiteSpace(request.ApiPassword))
        {
            dict["APIPassword"] = request.ApiPassword;
        }

        return dict;
    }

    private static CardcomGetLpResultResponse MapGetLpResultResponse(string raw)
    {
        var dict = ParseQueryString(raw);
        var response = new CardcomGetLpResultResponse
        {
            Raw = raw
        };

        if (dict.TryGetValue("ResponseCode", out var code) && int.TryParse(code, out var parsedCode))
        {
            response.ResponseCode = parsedCode;
        }

        dict.TryGetValue("Description", out var description);
        dict.TryGetValue("Token", out var token);
        dict.TryGetValue("ApproveNumber", out var approve);
        dict.TryGetValue("DealResponse", out var dealResponse);
        dict.TryGetValue("CardType", out var cardType);
        dict.TryGetValue("L4digit", out var l4digit);
        dict.TryGetValue("ReturnValue", out var returnValue);
        dict.TryGetValue("InternalDealNumber", out var internalDeal);
        dict.TryGetValue("ErrorText", out var errorText);
        dict.TryGetValue("InvoiceNumber", out var invoice);

        response.Description = description;
        response.Token = token;
        response.ApproveNumber = approve;
        response.DealResponse = dealResponse;
        response.CardType = cardType;
        response.L4digit = l4digit;
        response.ReturnValue = returnValue;
        response.InternalDealNumber = internalDeal;
        response.ErrorText = errorText;
        response.InvoiceNumber = invoice;

        if (dict.TryGetValue("CardValidityMonth", out var month) && int.TryParse(month, out var monthVal))
        {
            response.CardValidityMonth = monthVal;
        }

        if (dict.TryGetValue("CardValidityYear", out var year) && int.TryParse(year, out var yearVal))
        {
            response.CardValidityYear = yearVal;
        }

        return response;
    }

    private static Dictionary<string, string> ParseQueryString(string raw)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return result;
        }

        var parts = raw.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            var key = Uri.UnescapeDataString(kv[0]);
            var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }
}
