using System;
using FunderPayments.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FunderPayments.Migrations;

[DbContext(typeof(FunderDbContext))]
partial class FunderDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("FunderPayments.Models.Domain.PaymentToken", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uniqueidentifier");

            b.Property<string>("ApproveNumber")
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.Property<int>("CoinId")
                .HasColumnType("int");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("datetime2");

            b.Property<bool>("IsActive")
                .HasColumnType("bit");

            b.Property<decimal?>("MonthlyAmount")
                .HasColumnType("decimal(18,2)");

            b.Property<string>("Token")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");

            b.Property<DateTime>("UpdatedAt")
                .HasColumnType("datetime2");

            b.Property<string>("UserId")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");

            b.HasKey("Id");

            b.HasIndex("UserId", "Token")
                .IsUnique();

            b.ToTable("PaymentTokens");
        });

        modelBuilder.Entity("FunderPayments.Models.Domain.BillingHistory", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uniqueidentifier");

            b.Property<decimal>("Amount")
                .HasColumnType("decimal(18,2)");

            b.Property<string>("ApproveNumber")
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.Property<DateTime>("AttemptedAt")
                .HasColumnType("datetime2");

            b.Property<int>("CoinId")
                .HasColumnType("int");

            b.Property<string>("DealResponse")
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");

            b.Property<string>("Description")
                .HasColumnType("nvarchar(max)");

            b.Property<string>("Error")
                .HasColumnType("nvarchar(max)");

            b.Property<string>("InternalDealNumber")
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            b.Property<string>("OrderId")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("nvarchar(128)");

            b.Property<int>("ResponseCode")
                .HasColumnType("int");

            b.Property<string>("RawRequest")
                .HasColumnType("nvarchar(max)");

            b.Property<string>("RawResponse")
                .HasColumnType("nvarchar(max)");

            b.Property<bool>("Succeeded")
                .HasColumnType("bit");

            b.Property<Guid>("TokenId")
                .HasColumnType("uniqueidentifier");

            b.Property<string>("UserId")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");

            b.HasKey("Id");

            b.HasIndex("OrderId")
                .IsUnique();

            b.HasIndex("TokenId");

            b.ToTable("BillingHistories");
        });

        modelBuilder.Entity("FunderPayments.Models.Domain.BillingHistory", b =>
        {
            b.HasOne("FunderPayments.Models.Domain.PaymentToken", "Token")
                .WithMany("BillingHistories")
                .HasForeignKey("TokenId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Token");
        });

        modelBuilder.Entity("FunderPayments.Models.Domain.PaymentToken", b =>
        {
            b.Navigation("BillingHistories");
        });
#pragma warning restore 612, 618
    }
}


