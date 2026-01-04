using FunderPayments.Data;
using FunderPayments.Models.Requests;
using FunderPayments.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FunderPayments.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly FunderDbContext _dbContext;
    private readonly BillingService _billingService;
    private readonly ILogger<BillingController> _logger;

    public BillingController(FunderDbContext dbContext, BillingService billingService, ILogger<BillingController> logger)
    {
        _dbContext = dbContext;
        _billingService = billingService;
        _logger = logger;
    }

    [HttpPost("charge")]
    public async Task<IActionResult> Charge([FromBody] ManualChargeRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be greater than zero.");
        }

        var token = await ResolveTokenAsync(request, cancellationToken);
        if (token is null)
        {
            return NotFound("Active token not found for user.");
        }

        var coinId = request.CoinId > 0 ? request.CoinId : token.CoinId;
        var response = await _billingService.ChargeTokenAsync(token, request.Amount, coinId, cancellationToken: cancellationToken);

        return Ok(new
        {
            response.ResponseCode,
            response.Description,
            response.ApproveNumber,
            response.InternalDealNumber,
            response.DealResponse,
            response.Raw
        });
    }

    [HttpGet("tokens")]
    public async Task<IActionResult> Tokens([FromQuery] string? userId, CancellationToken cancellationToken)
    {
        var query = _dbContext.PaymentTokens.AsQueryable();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(t => t.UserId == userId);
        }

        var tokens = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.UserId,
                t.IsActive,
                t.MonthlyAmount,
                t.CoinId,
                t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(tokens);
    }

    [HttpGet("billing-history")]
    public async Task<IActionResult> History([FromQuery] string? userId, CancellationToken cancellationToken)
    {
        var query = _dbContext.BillingHistories.AsQueryable();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(h => h.UserId == userId);
        }

        var history = await query
            .OrderByDescending(h => h.AttemptedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return Ok(history);
    }

    [HttpPatch("tokens/{tokenId}/monthly-amount")]
    public async Task<IActionResult> UpdateMonthlyAmount([FromRoute] Guid tokenId, [FromBody] UpdateMonthlyAmountRequest request, CancellationToken cancellationToken)
    {
        var token = await _dbContext.PaymentTokens.FindAsync(new object?[] { tokenId }, cancellationToken);
        if (token is null)
        {
            return NotFound("Token not found.");
        }

        token.MonthlyAmount = request.MonthlyAmount;
        token.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            token.Id,
            token.UserId,
            token.MonthlyAmount,
            token.IsActive
        });
    }

    private async Task<FunderPayments.Models.Domain.PaymentToken?> ResolveTokenAsync(ManualChargeRequest request, CancellationToken cancellationToken)
    {
        if (request.TokenId.HasValue)
        {
            var token = await _dbContext.PaymentTokens.FindAsync(new object?[] { request.TokenId.Value }, cancellationToken: cancellationToken);
            if (token is not null)
            {
                return token;
            }
        }

        return await _dbContext.PaymentTokens
            .Where(t => t.UserId == request.UserId && t.IsActive)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
