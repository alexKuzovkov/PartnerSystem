using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommissionService.Infrastructure.Migrations;

[DbContext(typeof(CommissionDbContext))]
[Migration("20260815124000_RemoveUnusedCommissionPaymentState")]
public sealed class RemoveUnusedCommissionPaymentState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Commissions_IsPaid",
            table: "Commissions");

        migrationBuilder.DropColumn(
            name: "IsPaid",
            table: "Commissions");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsPaid",
            table: "Commissions",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            name: "IX_Commissions_IsPaid",
            table: "Commissions",
            column: "IsPaid");
    }
}
