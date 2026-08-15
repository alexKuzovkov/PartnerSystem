using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletService.Infrastructure.Migrations;

[DbContext(typeof(WalletDbContext))]
[Migration("20260815123000_FixPendingPayoutIdempotency")]
public sealed class FixPendingPayoutIdempotency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PendingPayouts_CommissionEventExternalId",
            table: "PendingPayouts");

        migrationBuilder.CreateIndex(
            name: "IX_PendingPayouts_CommissionEventExternalId_PartnerExternalId_Level",
            table: "PendingPayouts",
            columns: new[] { "CommissionEventExternalId", "PartnerExternalId", "Level" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PendingPayouts_CommissionEventExternalId_PartnerExternalId_Level",
            table: "PendingPayouts");

        migrationBuilder.CreateIndex(
            name: "IX_PendingPayouts_CommissionEventExternalId",
            table: "PendingPayouts",
            column: "CommissionEventExternalId",
            unique: true);
    }
}
