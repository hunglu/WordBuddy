using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBuddy.Content.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TargetAgeGroup = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GrammarRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Examples = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrammarRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrammarRules_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyPhrases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Phrase = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Translation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AudioAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VideoAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyPhrases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyPhrases_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailyPhrases_MediaAssets_AudioAssetId",
                        column: x => x.AudioAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DailyPhrases_MediaAssets_VideoAssetId",
                        column: x => x.VideoAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VocabularyItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Word = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Example = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AudioAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabularyItems_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VocabularyItems_MediaAssets_AudioAssetId",
                        column: x => x.AudioAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyPhrases_AudioAssetId",
                table: "DailyPhrases",
                column: "AudioAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPhrases_LessonId",
                table: "DailyPhrases",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPhrases_VideoAssetId",
                table: "DailyPhrases",
                column: "VideoAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_GrammarRules_LessonId",
                table: "GrammarRules",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyItems_AudioAssetId",
                table: "VocabularyItems",
                column: "AudioAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyItems_LessonId",
                table: "VocabularyItems",
                column: "LessonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyPhrases");

            migrationBuilder.DropTable(
                name: "GrammarRules");

            migrationBuilder.DropTable(
                name: "VocabularyItems");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "MediaAssets");
        }
    }
}
