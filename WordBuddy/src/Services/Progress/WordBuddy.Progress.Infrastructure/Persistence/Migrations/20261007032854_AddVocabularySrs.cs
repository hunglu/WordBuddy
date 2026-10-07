using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBuddy.Progress.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVocabularySrs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearnerWordStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Stability = table.Column<double>(type: "float", nullable: false),
                    Difficulty = table.Column<double>(type: "float", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reps = table.Column<int>(type: "int", nullable: false),
                    Lapses = table.Column<int>(type: "int", nullable: false),
                    FsrsPhase = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FsrsStep = table.Column<int>(type: "int", nullable: true),
                    LastReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FirstReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerWordStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReviewLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExerciseType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Skill = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    ResponseMs = table.Column<int>(type: "int", nullable: false),
                    HintUsed = table.Column<bool>(type: "bit", nullable: false),
                    IsDue = table.Column<bool>(type: "bit", nullable: false),
                    AttemptNo = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyLearnerSettings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewWordsPerDay = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyLearnerSettings", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearnerWordStates_UserId_IsActive_DueAtUtc",
                table: "LearnerWordStates",
                columns: new[] { "UserId", "IsActive", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LearnerWordStates_UserId_SenseId",
                table: "LearnerWordStates",
                columns: new[] { "UserId", "SenseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewLogs_UserId_OccurredAtUtc",
                table: "ReviewLogs",
                columns: new[] { "UserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewLogs_UserId_SenseId",
                table: "ReviewLogs",
                columns: new[] { "UserId", "SenseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearnerWordStates");

            migrationBuilder.DropTable(
                name: "ReviewLogs");

            migrationBuilder.DropTable(
                name: "VocabularyLearnerSettings");
        }
    }
}
