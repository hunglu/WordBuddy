using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBuddy.Content.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalVocabularyWords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonalVocabularyWords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerAgeGroup = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Word = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Example = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShareStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VisibleToChildren = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModeratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModeratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalVocabularyWords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalVocabularyWords_OwnerUserId",
                table: "PersonalVocabularyWords",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalVocabularyWords_ShareStatus",
                table: "PersonalVocabularyWords",
                column: "ShareStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonalVocabularyWords");
        }
    }
}
