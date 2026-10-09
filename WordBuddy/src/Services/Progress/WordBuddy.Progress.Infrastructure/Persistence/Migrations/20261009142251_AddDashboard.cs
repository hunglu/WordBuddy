using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBuddy.Progress.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VocabularySessionIssues",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlannedCount = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularySessionIssues", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "VocabularySessionIssueItems",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsNew = table.Column<bool>(type: "bit", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularySessionIssueItems", x => new { x.SessionId, x.SenseId });
                    table.ForeignKey(
                        name: "FK_VocabularySessionIssueItems_VocabularySessionIssues_SessionId",
                        column: x => x.SessionId,
                        principalTable: "VocabularySessionIssues",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VocabularySessionIssues_UserId_IssuedAtUtc",
                table: "VocabularySessionIssues",
                columns: new[] { "UserId", "IssuedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VocabularySessionIssueItems");

            migrationBuilder.DropTable(
                name: "VocabularySessionIssues");
        }
    }
}
