using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommissionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Commissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventExternalId = table.Column<string>(type: "text", nullable: false),
                    PartnerExternalId = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SchemaType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commissions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_EventExternalId_PartnerExternalId_Level",
                table: "Commissions",
                columns: new[] { "EventExternalId", "PartnerExternalId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_IsPaid",
                table: "Commissions",
                column: "IsPaid");

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_PartnerExternalId",
                table: "Commissions",
                column: "PartnerExternalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Commissions");
        }
    }
}
