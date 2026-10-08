using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBuddy.Content.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportLinkProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupportLinkProjections",
                columns: table => new
                {
                    LinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupporterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportLinkProjections", x => x.LinkId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportLinkProjections_LearnerId_IsActive",
                table: "SupportLinkProjections",
                columns: new[] { "LearnerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportLinkProjections_SupporterId_LearnerId",
                table: "SupportLinkProjections",
                columns: new[] { "SupporterId", "LearnerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportLinkProjections");
        }
    }
}
