using FunderPayments.Data;
using FunderPayments.Models.Domain;
using FunderPayments.Models.Options;
using FunderPayments.Models.Requests;
using FunderPayments.Models.Requests.FunderApi;
using FunderPayments.Models.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FunderPayments.Services;

public class CallbackService
{
    private readonly FunderDbContext _dbContext;
    private readonly FunderApiClient _funderApiClient;
    private readonly CardcomClient _cardcomClient;
    private readonly CardcomOptions _cardcomOptions;
    private readonly ILogger<CallbackService> _logger;

    public CallbackService(
        FunderDbContext dbContext, 
        FunderApiClient funderApiClient,
        CardcomClient cardcomClient,
        IOptions<CardcomOptions> cardcomOptions,
        ILogger<CallbackService> logger)
    {
        _dbContext = dbContext;
        _funderApiClient = funderApiClient;
        _cardcomClient = cardcomClient;
        _cardcomOptions = cardcomOptions.Value;
        _logger = logger;
    }

    public async Task ProcessCallbackAsync(CardcomTokenResponse callback, string userId, decimal? monthlyAmount = null, IFormCollection? rawForm = null, CancellationToken cancellationToken = default)
    {
        // Step 1: Basic validation of callback
        if (callback.ResponseCode != 0 || string.IsNullOrWhiteSpace(callback.Token))
        {
            _logger.LogWarning("Cardcom callback failure. Code: {Code}, Error: {Error}", callback.ResponseCode, callback.ErrorText);
            return;
        }

        // Step 2: CRITICAL - Verify transaction data via GetLpResult
        // This prevents fraud and ensures data integrity
        if (string.IsNullOrWhiteSpace(callback.LowProfileId))
        {
            _logger.LogError("LowProfileId is missing from callback. Cannot verify transaction for user {UserId}", userId);
            return;
        }

        var verifiedData = await VerifyTransactionAsync(callback.LowProfileId, cancellationToken);
        if (verifiedData is null || verifiedData.ResponseCode != 0)
        {
            _logger.LogError("GetLpResult verification failed. ResponseCode: {Code}, Description: {Desc}", 
                verifiedData?.ResponseCode, verifiedData?.Description);
            return;
        }

        // Step 3: Check if token already exists (prevent duplicates)
        var tokenEntity = await _dbContext.PaymentTokens
            .FirstOrDefaultAsync(t => t.Token == verifiedData.Token && t.UserId == userId, cancellationToken);

        // Prevent duplicate processing if already verified
        if (tokenEntity is not null && tokenEntity.IsVerified)
        {
            _logger.LogInformation("Token {Token} for user {UserId} already verified. Skipping duplicate processing.", 
                verifiedData.Token, userId);
            return;
        }

        var isNewToken = tokenEntity is null;

        // Step 4: Save verified token data
        if (tokenEntity is null)
        {
            tokenEntity = new PaymentToken
            {
                UserId = userId,
                Token = verifiedData.Token ?? string.Empty,
                ApproveNumber = verifiedData.ApproveNumber,
                IsActive = true,
                IsVerified = true, // Mark as verified after GetLpResult
                MonthlyAmount = monthlyAmount
            };
            _dbContext.PaymentTokens.Add(tokenEntity);
        }
        else
        {
            tokenEntity.ApproveNumber = verifiedData.ApproveNumber;
            tokenEntity.IsActive = true;
            tokenEntity.IsVerified = true; // Mark as verified
            if (monthlyAmount.HasValue)
            {
                tokenEntity.MonthlyAmount = monthlyAmount.Value;
            }
            tokenEntity.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Stored and verified token for user {UserId}", userId);

        // Step 5: Integration #1 - Notify FUNDER API that token was registered
        if (isNewToken)
        {
            await NotifyFunderTokenRegisteredAsync(tokenEntity, verifiedData, cancellationToken);
        }
    }

    /// <summary>
    /// CRITICAL: Verify transaction data by calling GetLpResult
    /// This is mandatory for security - never trust webhook data alone
    /// </summary>
    private async Task<CardcomGetLpResultResponse?> VerifyTransactionAsync(string lowProfileId, CancellationToken cancellationToken)
    {
        try
        {
            var request = new CardcomGetLpResultRequest
            {
                TerminalNumber = _cardcomOptions.TerminalNumber,
                UserName = _cardcomOptions.UserName,
                LowProfileId = lowProfileId,
                ApiPassword = _cardcomOptions.ApiPassword
            };

            var verifiedData = await _cardcomClient.GetLpResultAsync(request, cancellationToken);
            _logger.LogInformation("GetLpResult verification successful. ResponseCode: {Code}, Token: {Token}", 
                verifiedData.ResponseCode, verifiedData.Token);

            return verifiedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GetLpResult for LowProfileId {LowProfileId}", lowProfileId);
            return null;
        }
    }

    private async Task NotifyFunderTokenRegisteredAsync(PaymentToken token, CardcomGetLpResultResponse verifiedData, CancellationToken cancellationToken)
    {
        try
        {
            var request = new PaymentTokenRegisteredRequest
            {
                UserId = token.UserId,
                Token = token.Token,
                CardType = verifiedData.CardType, // From verified data, not stored in DB
                Last4Digits = verifiedData.L4digit, // From verified data, not stored in DB
                MonthlyAmount = token.MonthlyAmount ?? 0m,
                Status = token.IsActive ? "Active" : "Inactive",
                RegisteredAt = token.CreatedAt
            };

            var success = await _funderApiClient.NotifyTokenRegisteredAsync(request, cancellationToken);
            if (!success)
            {
                _logger.LogWarning("Failed to notify FUNDER API about token registration for user {UserId}", token.UserId);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the callback if FUNDER API notification fails
            _logger.LogError(ex, "Error notifying FUNDER API about token registration for user {UserId}", token.UserId);
        }
    }
}
