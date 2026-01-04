using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FunderPayments.Models.Domain;

public class BillingHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(256)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public Guid TokenId { get; set; }

    [Required]
    [MaxLength(128)]
    public string OrderId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public int CoinId { get; set; } = 1;

    public int ResponseCode { get; set; }
    public string? Description { get; set; }
    [MaxLength(50)]
    public string? ApproveNumber { get; set; }

    [MaxLength(100)]
    public string? InternalDealNumber { get; set; }

    [MaxLength(200)]
    public string? DealResponse { get; set; }

    public bool Succeeded { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    public string? RawRequest { get; set; }
    public string? RawResponse { get; set; }
    public string? Error { get; set; }

    // Navigation
    public PaymentToken Token { get; set; } = null!;
}
