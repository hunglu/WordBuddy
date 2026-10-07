using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBuddy.Progress.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewConcurrencyGuards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LearnerWordStates",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewLogs_UserId_SessionId_SenseId_AttemptNo",
                table: "ReviewLogs",
                columns: new[] { "UserId", "SessionId", "SenseId", "AttemptNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReviewLogs_UserId_SessionId_SenseId_AttemptNo",
                table: "ReviewLogs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LearnerWordStates");
        }
    }
}
