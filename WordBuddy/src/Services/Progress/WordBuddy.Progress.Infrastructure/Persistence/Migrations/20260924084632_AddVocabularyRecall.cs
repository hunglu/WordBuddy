using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBuddy.Progress.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVocabularyRecall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VocabularyRecallSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WordsChecked = table.Column<int>(type: "int", nullable: false),
                    WordsKnown = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyRecallSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyRecallStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VocabularyWordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Word = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TimesChecked = table.Column<int>(type: "int", nullable: false),
                    TimesKnown = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastCheckedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyRecallStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyRecallSessions_UserId_CheckedAtUtc",
                table: "VocabularyRecallSessions",
                columns: new[] { "UserId", "CheckedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyRecallStats_UserId_VocabularyWordId",
                table: "VocabularyRecallStats",
                columns: new[] { "UserId", "VocabularyWordId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VocabularyRecallSessions");

            migrationBuilder.DropTable(
                name: "VocabularyRecallStats");
        }
    }
}
