using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingPayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommissionEventExternalId = table.Column<string>(type: "text", nullable: false),
                    PartnerExternalId = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingPayouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserExternalId = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CommissionEventExternalId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserExternalId = table.Column<string>(type: "text", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CreatedAt",
                table: "Transactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserExternalId",
                table: "Transactions",
                column: "UserExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserExternalId_CommissionEventExternalId",
                table: "Transactions",
                columns: new[] { "UserExternalId", "CommissionEventExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserExternalId",
                table: "Wallets",
                column: "UserExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingPayouts");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Wallets");
        }
    }
}
