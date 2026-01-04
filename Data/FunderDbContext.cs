using FunderPayments.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace FunderPayments.Data;

public class FunderDbContext : DbContext
{
    public FunderDbContext(DbContextOptions<FunderDbContext> options) : base(options)
    {
    }

    public DbSet<PaymentToken> PaymentTokens => Set<PaymentToken>();
    public DbSet<BillingHistory> BillingHistories => Set<BillingHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentToken>(entity =>
        {
            entity.HasIndex(t => new { t.UserId, t.Token })
                .IsUnique();

            entity.HasMany(t => t.BillingHistories)
                .WithOne(h => h.Token)
                .HasForeignKey(h => h.TokenId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BillingHistory>(entity =>
        {
            entity.HasIndex(h => h.OrderId)
                .IsUnique();
        });
    }
}
