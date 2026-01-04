using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FunderPayments.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PaymentTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Token = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                ApproveNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                MonthlyAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                CoinId = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentTokens", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "BillingHistories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                TokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                CoinId = table.Column<int>(type: "int", nullable: false),
                ResponseCode = table.Column<int>(type: "int", nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ApproveNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                InternalDealNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                DealResponse = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                Succeeded = table.Column<bool>(type: "bit", nullable: false),
                AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                RawRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                RawResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BillingHistories", x => x.Id);
                table.ForeignKey(
                    name: "FK_BillingHistories_PaymentTokens_TokenId",
                    column: x => x.TokenId,
                    principalTable: "PaymentTokens",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PaymentTokens_UserId_Token",
            table: "PaymentTokens",
            columns: new[] { "UserId", "Token" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BillingHistories_OrderId",
            table: "BillingHistories",
            column: "OrderId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BillingHistories_TokenId",
            table: "BillingHistories",
            column: "TokenId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BillingHistories");

        migrationBuilder.DropTable(
            name: "PaymentTokens");
    }
}


