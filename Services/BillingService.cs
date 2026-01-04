using FunderPayments.Data;
using FunderPayments.Models.Domain;
using FunderPayments.Models.Options;
using FunderPayments.Models.Requests;
using FunderPayments.Models.Requests.FunderApi;
using FunderPayments.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FunderPayments.Services;

public class BillingService
{
    private readonly CardcomClient _client;
    private readonly CardcomOptions _options;
    private readonly FunderDbContext _dbContext;
    private readonly FunderApiClient _funderApiClient;
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        CardcomClient client, 
        IOptions<CardcomOptions> options, 
        FunderDbContext dbContext,
        FunderApiClient funderApiClient,
        ILogger<BillingService> logger)
    {
        _client = client;
        _options = options.Value;
        _dbContext = dbContext;
        _funderApiClient = funderApiClient;
        _logger = logger;
    }

    public async Task<CardcomChargeResponse> ChargeTokenAsync(PaymentToken token, decimal amount, int coinId, string? orderId = null, CancellationToken cancellationToken = default)
    {
        var order = orderId ?? $"{token.UserId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var request = new CardcomChargeRequest
        {
            TerminalNumber = _options.TerminalNumber,
            UserName = _options.UserName,
            APILevel = _options.ApiLevel,
            Operation = 4,
            Token = token.Token,
            Sum = amount,
            CoinID = coinId,
            Order = order,
            APIPassword = _options.ApiPassword
        };

        CardcomChargeResponse response;
        try
        {
            response = await _client.ChargeTokenAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Do-Transaction call failed for order {Order}", order);
            response = new CardcomChargeResponse
            {
                ResponseCode = -1,
                Description = ex.Message,
                Raw = ex.ToString()
            };
        }

        await PersistHistoryAsync(token, request, response, amount, coinId, order, cancellationToken);
        return response;
    }

    public async Task RunMonthlyBillingAsync(CancellationToken cancellationToken = default)
    {
        // Integration #2: Get eligible users from FUNDER API
        var eligibleResponse = await _funderApiClient.GetEligibleUsersAsync(cancellationToken);
        
        if (eligibleResponse is null || eligibleResponse.EligibleUsers.Count == 0)
        {
            _logger.LogInformation("No eligible users returned from FUNDER API for monthly billing");
            return;
        }

        _logger.LogInformation("FUNDER API returned {Count} eligible users for billing", eligibleResponse.EligibleUsers.Count);

        // Build a dictionary of eligible users by UserId+Token for quick lookup
        var eligibleLookup = eligibleResponse.EligibleUsers
            .Where(u => u.IsEligible && !string.IsNullOrWhiteSpace(u.UserId) && !string.IsNullOrWhiteSpace(u.Token))
            .ToDictionary(u => $"{u.UserId}|{u.Token}", u => u);

        // Get all active tokens from our database
        var tokens = await _dbContext.PaymentTokens
            .Where(t => t.IsActive && t.MonthlyAmount.HasValue && t.MonthlyAmount.Value > 0)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} active tokens in database", tokens.Count);

        int billedCount = 0;
        int skippedCount = 0;

        foreach (var token in tokens)
        {
            var lookupKey = $"{token.UserId}|{token.Token}";
            
            // Check if user is eligible according to FUNDER API
            if (!eligibleLookup.TryGetValue(lookupKey, out var eligibleUser))
            {
                _logger.LogDebug("User {UserId} with token {Token} is not eligible for billing (not in FUNDER API response)", 
                    token.UserId, token.Token);
                skippedCount++;
                continue;
            }

            // Use amount from FUNDER API if provided, otherwise use token's monthly amount
            var amount = eligibleUser.MonthlyAmount > 0 
                ? eligibleUser.MonthlyAmount 
                : (token.MonthlyAmount ?? _options.DefaultMonthlyAmount);
            
            var coinId = token.CoinId <= 0 ? _options.DefaultCoinId : token.CoinId;
            
            _logger.LogInformation("Charging user {UserId}, amount {Amount}, coin {CoinId}", token.UserId, amount, coinId);
            
            await ChargeTokenAsync(token, amount, coinId, cancellationToken: cancellationToken);
            billedCount++;
        }

        _logger.LogInformation("Monthly billing completed. Billed: {Billed}, Skipped: {Skipped}", billedCount, skippedCount);
    }

    private async Task PersistHistoryAsync(PaymentToken token, CardcomChargeRequest request, CardcomChargeResponse response, decimal amount, int coinId, string orderId, CancellationToken cancellationToken)
    {
        var history = new BillingHistory
        {
            UserId = token.UserId,
            TokenId = token.Id,
            OrderId = orderId,
            Amount = amount,
            CoinId = coinId,
            ResponseCode = response.ResponseCode,
            Description = response.Description,
            ApproveNumber = response.ApproveNumber,
            InternalDealNumber = response.InternalDealNumber,
            DealResponse = response.DealResponse,
            Succeeded = response.ResponseCode == 0,
            RawRequest = System.Text.Json.JsonSerializer.Serialize(request),
            RawResponse = response.Raw
        };

        if (response.ResponseCode != 0 && response.ResponseCode != -1)
        {
            history.Error = response.Description;
        }

        _dbContext.BillingHistories.Add(history);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Integration #3 & #4: Notify FUNDER API of billing result
        if (response.ResponseCode == 0)
        {
            // Integration #3: Success
            await NotifyFunderBillingSuccessAsync(token, response, amount, coinId, orderId, cancellationToken);
        }
        else
        {
            // Integration #4: Failure
            await NotifyFunderBillingFailedAsync(token, response, amount, coinId, orderId, cancellationToken);
        }
    }

    private async Task NotifyFunderBillingSuccessAsync(PaymentToken token, CardcomChargeResponse response, decimal amount, int coinId, string orderId, CancellationToken cancellationToken)
    {
        try
        {
            var request = new BillingSuccessRequest
            {
                UserId = token.UserId,
                Token = token.Token,
                Amount = amount,
                CoinId = coinId,
                OrderId = orderId,
                ApproveNumber = response.ApproveNumber ?? string.Empty,
                InternalDealNumber = response.InternalDealNumber,
                ChargedAt = DateTime.UtcNow
            };

            var success = await _funderApiClient.NotifyBillingSuccessAsync(request, cancellationToken);
            if (!success)
            {
                _logger.LogWarning("Failed to notify FUNDER API about billing success for user {UserId}, order {OrderId}", 
                    token.UserId, orderId);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the billing process if FUNDER API notification fails
            _logger.LogError(ex, "Error notifying FUNDER API about billing success for user {UserId}, order {OrderId}", 
                token.UserId, orderId);
        }
    }

    private async Task NotifyFunderBillingFailedAsync(PaymentToken token, CardcomChargeResponse response, decimal amount, int coinId, string orderId, CancellationToken cancellationToken)
    {
        try
        {
            var request = new BillingFailedRequest
            {
                UserId = token.UserId,
                Token = token.Token,
                Amount = amount,
                CoinId = coinId,
                OrderId = orderId,
                ResponseCode = response.ResponseCode,
                Description = response.Description,
                Error = response.Description,
                AttemptedAt = DateTime.UtcNow
            };

            var success = await _funderApiClient.NotifyBillingFailedAsync(request, cancellationToken);
            if (!success)
            {
                _logger.LogWarning("Failed to notify FUNDER API about billing failure for user {UserId}, order {OrderId}", 
                    token.UserId, orderId);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the billing process if FUNDER API notification fails
            _logger.LogError(ex, "Error notifying FUNDER API about billing failure for user {UserId}, order {OrderId}", 
                token.UserId, orderId);
        }
    }
}
