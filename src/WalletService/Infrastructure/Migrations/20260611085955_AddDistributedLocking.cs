using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDistributedLocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "PendingPayouts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedByInstance",
                table: "PendingPayouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PendingPayouts",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_PendingPayouts_CommissionEventExternalId",
                table: "PendingPayouts",
                column: "CommissionEventExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingPayouts_IsPaid_LockedAt",
                table: "PendingPayouts",
                columns: new[] { "IsPaid", "LockedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PendingPayouts_CommissionEventExternalId",
                table: "PendingPayouts");

            migrationBuilder.DropIndex(
                name: "IX_PendingPayouts_IsPaid_LockedAt",
                table: "PendingPayouts");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "PendingPayouts");

            migrationBuilder.DropColumn(
                name: "LockedByInstance",
                table: "PendingPayouts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PendingPayouts");
        }
    }
}
