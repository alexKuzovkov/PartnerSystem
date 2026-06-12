using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfitEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventExternalId = table.Column<string>(type: "text", nullable: false),
                    UserExternalId = table.Column<string>(type: "text", nullable: false),
                    Profit = table.Column<decimal>(type: "numeric", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedDate",
                table: "OutboxMessages",
                column: "ProcessedDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitEvents_EventExternalId",
                table: "ProfitEvents",
                column: "EventExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfitEvents_UserExternalId_OccurredAt",
                table: "ProfitEvents",
                columns: new[] { "UserExternalId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "ProfitEvents");
        }
    }
}
