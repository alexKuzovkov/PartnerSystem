using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletService.Infrastructure.Migrations;

[DbContext(typeof(WalletDbContext))]
[Migration("20260815125000_RemoveUnusedPayoutRowVersion")]
public sealed class RemoveUnusedPayoutRowVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "PendingPayouts");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "PendingPayouts",
            type: "bytea",
            rowVersion: true,
            nullable: false,
            defaultValue: Array.Empty<byte>());
    }
}
