using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FunderPayments.Models.Domain;

public class PaymentToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(256)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Token { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ApproveNumber { get; set; }

    public decimal? MonthlyAmount { get; set; }
    public int CoinId { get; set; } = 1;

    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; } = false; // Set to true after GetLpResult verification
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<BillingHistory> BillingHistories { get; set; } = new List<BillingHistory>();
}
