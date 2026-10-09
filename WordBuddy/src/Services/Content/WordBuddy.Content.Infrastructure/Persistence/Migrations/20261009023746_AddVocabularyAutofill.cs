using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBuddy.Content.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVocabularyAutofill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Senses_LexemeId",
                table: "Senses");

            migrationBuilder.AddColumn<string>(
                name: "Antonyms",
                table: "Senses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValueSql: "N'[]'");

            migrationBuilder.AddColumn<bool>(
                name: "ChildSuitableHint",
                table: "Senses",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Collocations",
                table: "Senses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValueSql: "N'[]'");

            migrationBuilder.AddColumn<string>(
                name: "Examples",
                table: "Senses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValueSql: "N'[]'");

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Senses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<string>(
                name: "RegisterNote",
                table: "Senses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Synonyms",
                table: "Senses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValueSql: "N'[]'");

            migrationBuilder.AddColumn<string>(
                name: "TopicTags",
                table: "Senses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValueSql: "N'[]'");

            migrationBuilder.AddColumn<DateTime>(
                name: "ChildApprovedAtUtc",
                table: "LearnerWords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChildApprovedByUserId",
                table: "LearnerWords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresChildApproval",
                table: "LearnerWords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Senses_LexemeId_Origin",
                table: "Senses",
                columns: new[] { "LexemeId", "Origin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Senses_LexemeId_Origin",
                table: "Senses");

            migrationBuilder.DropColumn(
                name: "Antonyms",
                table: "Senses");

            migrationBuilder.DropColumn(
                name: "ChildSuitableHint",
                table: "Senses");

            migrationBuilder.DropColumn(
                name: "Collocations",
                table: "Senses");

            migrationBuilder.DropColumn(
                name: "Examples",
                table: "Senses");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Senses");

            migrationBuilder.DropColumn(
                name: "RegisterNote",
                table: "Senses");

            migrationBuilder.DropColumn(
                name: "Synonyms",
                table: "Senses");

            migrationBuilder.DropColumn(
                name: "TopicTags",
                table: "Senses");

            migrationBuilder.DropColumn(
                name: "ChildApprovedAtUtc",
                table: "LearnerWords");

            migrationBuilder.DropColumn(
                name: "ChildApprovedByUserId",
                table: "LearnerWords");

            migrationBuilder.DropColumn(
                name: "RequiresChildApproval",
                table: "LearnerWords");

            migrationBuilder.CreateIndex(
                name: "IX_Senses_LexemeId",
                table: "Senses",
                column: "LexemeId");
        }
    }
}
