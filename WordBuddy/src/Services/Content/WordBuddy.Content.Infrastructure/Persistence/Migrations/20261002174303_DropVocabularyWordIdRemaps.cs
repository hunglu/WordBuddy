using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBuddy.Content.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropVocabularyWordIdRemaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VocabularyWordIdRemaps");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VocabularyWordIdRemaps",
                columns: table => new
                {
                    OldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyWordIdRemaps", x => x.OldId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyWordIdRemaps_PublishedAtUtc",
                table: "VocabularyWordIdRemaps",
                column: "PublishedAtUtc");
        }
    }
}
