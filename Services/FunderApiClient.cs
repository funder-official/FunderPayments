using FunderPayments.Models.Options;
using FunderPayments.Models.Requests.FunderApi;
using FunderPayments.Models.Responses.FunderApi;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FunderPayments.Services;

public class FunderApiClient
{
    private readonly HttpClient _httpClient;
    private readonly FunderApiOptions _options;
    private readonly ILogger<FunderApiClient> _logger;

    public FunderApiClient(HttpClient httpClient, IOptions<FunderApiOptions> options, ILogger<FunderApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Integration #1: Notify FUNDER that a payment token was registered
    /// </summary>
    public async Task<bool> NotifyTokenRegisteredAsync(PaymentTokenRegisteredRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = BuildEndpoint(_options.PaymentTokenRegisteredPath);
            _logger.LogInformation("Notifying FUNDER API: Token registered for user {UserId}", request.UserId);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully notified FUNDER API: Token registered for user {UserId}", request.UserId);
                return true;
            }
            else
            {
                _logger.LogWarning("FUNDER API notification failed. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying FUNDER API: Token registered for user {UserId}", request.UserId);
            return false;
        }
    }

    /// <summary>
    /// Integration #2: Get list of users eligible for monthly billing
    /// </summary>
    public async Task<EligibleForBillingResponse?> GetEligibleUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = BuildEndpoint(_options.EligibleForBillingPath);
            _logger.LogInformation("Querying FUNDER API: Eligible users for billing");

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<EligibleForBillingResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("FUNDER API returned {Count} eligible users", result?.EligibleUsers?.Count ?? 0);
                return result;
            }
            else
            {
                _logger.LogWarning("FUNDER API eligibility check failed. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying FUNDER API: Eligible users for billing");
            return null;
        }
    }

    /// <summary>
    /// Integration #3: Notify FUNDER that billing succeeded
    /// </summary>
    public async Task<bool> NotifyBillingSuccessAsync(BillingSuccessRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = BuildEndpoint(_options.BillingSuccessPath);
            _logger.LogInformation("Notifying FUNDER API: Billing success for user {UserId}, Order {OrderId}", request.UserId, request.OrderId);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully notified FUNDER API: Billing success for user {UserId}", request.UserId);
                return true;
            }
            else
            {
                _logger.LogWarning("FUNDER API notification failed. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying FUNDER API: Billing success for user {UserId}", request.UserId);
            return false;
        }
    }

    /// <summary>
    /// Integration #4: Notify FUNDER that billing failed
    /// </summary>
    public async Task<bool> NotifyBillingFailedAsync(BillingFailedRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = BuildEndpoint(_options.BillingFailedPath);
            _logger.LogInformation("Notifying FUNDER API: Billing failed for user {UserId}, Order {OrderId}, Code {Code}", 
                request.UserId, request.OrderId, request.ResponseCode);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully notified FUNDER API: Billing failed for user {UserId}", request.UserId);
                return true;
            }
            else
            {
                _logger.LogWarning("FUNDER API notification failed. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying FUNDER API: Billing failed for user {UserId}", request.UserId);
            return false;
        }
    }

    private Uri BuildEndpoint(string path)
    {
        var baseUri = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        return new Uri(baseUri, path.TrimStart('/'));
    }
}

