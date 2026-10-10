using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBuddy.Progress.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServerAnswerChecking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientResponseMs",
                table: "ReviewLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExerciseId",
                table: "ReviewLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServerResponseMs",
                table: "ReviewLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TimingAdjusted",
                table: "ReviewLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "VocabularyExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Skill = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExpectedAnswer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CorrectWord = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Options = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnsweredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyExercises", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewLogs_ExerciseId",
                table: "ReviewLogs",
                column: "ExerciseId",
                unique: true,
                filter: "[ExerciseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyExercises_SessionId",
                table: "VocabularyExercises",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyExercises_UserId",
                table: "VocabularyExercises",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VocabularyExercises");

            migrationBuilder.DropIndex(
                name: "IX_ReviewLogs_ExerciseId",
                table: "ReviewLogs");

            migrationBuilder.DropColumn(
                name: "ClientResponseMs",
                table: "ReviewLogs");

            migrationBuilder.DropColumn(
                name: "ExerciseId",
                table: "ReviewLogs");

            migrationBuilder.DropColumn(
                name: "ServerResponseMs",
                table: "ReviewLogs");

            migrationBuilder.DropColumn(
                name: "TimingAdjusted",
                table: "ReviewLogs");
        }
    }
}
