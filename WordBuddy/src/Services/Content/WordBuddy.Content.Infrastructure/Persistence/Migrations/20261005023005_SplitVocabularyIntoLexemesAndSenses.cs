using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBuddy.Content.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Splits <c>VocabularyWords</c> into <c>Lexemes</c> + <c>Senses</c> (ADR 0004), renames
    /// <c>UserVocabularyWords</c> to <c>LearnerWords</c> and <c>LessonVocabularyWords</c> to
    /// <c>LessonSenses</c>, and adds <c>SenseTranslations</c>. Hand-edited: tables, columns, indexes
    /// and PK/FK constraints are <b>renamed in place</b> (<c>sp_rename</c>); no row is copied, so every
    /// id, <c>ContentHash</c>, link and <c>SortOrder</c> stays byte-identical.
    /// <list type="number">
    /// <item>Create <c>Lexemes</c>.</item>
    /// <item>Insert one lexeme per distinct <c>NormalizedWord</c> (database collation), with
    /// <c>PartOfSpeech = NULL</c>. Lemma = trimmed <c>Word</c> of the preferred row: System, then
    /// Shared, then oldest <c>CreatedAtUtc</c>, then <c>Id</c>.</item>
    /// <item>Add <c>LexemeId</c>, fill it by joining on <c>NormalizedWord</c>, make it NOT NULL.</item>
    /// <item>Create the unfiltered unique (<c>NormalizedLemma</c>, <c>PartOfSpeech</c>) index, the
    /// <c>LexemeId</c> index and FK.</item>
    /// <item>Rename the three tables, the link columns, and their PK/FK/index names.</item>
    /// <item>Drop <c>NormalizedWord</c> and its index.</item>
    /// <item>Add <c>LearnerWords.AddedBy</c> (default <c>'Learner'</c>) and <c>PersonalContext</c>.</item>
    /// <item>Create <c>SenseTranslations</c>.</item>
    /// </list>
    /// <para>One transaction (no <c>suppressTransaction</c>): a failure rolls back cleanly.</para>
    /// <para><b>Down is lossy</b> for lexeme fields, translations and <c>PersonalContext</c> (all empty
    /// right after Up). It re-fills <c>NormalizedWord</c> as <c>UPPER(LTRIM(RTRIM(Word)))</c> and
    /// renames everything back; all ids are kept.</para>
    /// </summary>
    public partial class SplitVocabularyIntoLexemesAndSenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Lexemes (unique index comes after the data, so step 2 is validated by it).
            migrationBuilder.CreateTable(
                name: "Lexemes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Lemma = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedLemma = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PartOfSpeech = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IpaUk = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpaUs = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UkAudioAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsAudioAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Syllables = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WordForms = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValueSql: "N'[]'"),
                    CefrLevel = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    FrequencyRank = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lexemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lexemes_MediaAssets_UkAudioAssetId",
                        column: x => x.UkAudioAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Lexemes_MediaAssets_UsAudioAssetId",
                        column: x => x.UsAudioAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lexemes_UkAudioAssetId",
                table: "Lexemes",
                column: "UkAudioAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Lexemes_UsAudioAssetId",
                table: "Lexemes",
                column: "UsAudioAssetId");

            // 2. One lexeme per distinct NormalizedWord, PartOfSpeech unknown.
            migrationBuilder.Sql("""
                INSERT INTO [Lexemes] ([Id], [Lemma], [NormalizedLemma], [PartOfSpeech], [CreatedAtUtc])
                SELECT NEWID(), LTRIM(RTRIM(p.[Word])), g.[NormalizedWord], NULL, g.[FirstCreatedAtUtc]
                FROM (
                    SELECT [NormalizedWord], MIN([CreatedAtUtc]) AS [FirstCreatedAtUtc]
                    FROM [VocabularyWords]
                    GROUP BY [NormalizedWord]
                ) AS g
                CROSS APPLY (
                    SELECT TOP (1) v.[Word]
                    FROM [VocabularyWords] AS v
                    WHERE v.[NormalizedWord] = g.[NormalizedWord]
                    ORDER BY
                        CASE WHEN v.[Source] = N'System' THEN 0
                             WHEN v.[ShareStatus] = N'Shared' THEN 1
                             ELSE 2 END,
                        v.[CreatedAtUtc],
                        v.[Id]
                ) AS p;
                """);

            // 3. LexemeId on every sense.
            migrationBuilder.AddColumn<Guid>(
                name: "LexemeId",
                table: "VocabularyWords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE v SET v.[LexemeId] = l.[Id]
                FROM [VocabularyWords] AS v
                INNER JOIN [Lexemes] AS l
                    ON l.[NormalizedLemma] = v.[NormalizedWord] AND l.[PartOfSpeech] IS NULL;
                """);

            migrationBuilder.Sql("ALTER TABLE [VocabularyWords] ALTER COLUMN [LexemeId] uniqueidentifier NOT NULL;");

            // 4. Unique lexeme key WITHOUT a filter: (APPLE, NULL) may exist only once.
            migrationBuilder.CreateIndex(
                name: "UX_Lexemes_NormalizedLemma_PartOfSpeech",
                table: "Lexemes",
                columns: new[] { "NormalizedLemma", "PartOfSpeech" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Senses_LexemeId",
                table: "VocabularyWords",
                column: "LexemeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Senses_Lexemes_LexemeId",
                table: "VocabularyWords",
                column: "LexemeId",
                principalTable: "Lexemes",
                principalColumn: "Id");

            // 5. Rename tables, columns, PK/FK constraints and indexes (metadata only).
            RenameObject(migrationBuilder, "PK_VocabularyWords", "PK_Senses");
            RenameObject(migrationBuilder, "FK_VocabularyWords_MediaAssets_AudioAssetId", "FK_Senses_MediaAssets_AudioAssetId");
            RenameObject(migrationBuilder, "PK_UserVocabularyWords", "PK_LearnerWords");
            RenameObject(migrationBuilder, "FK_UserVocabularyWords_VocabularyWords_VocabularyWordId", "FK_LearnerWords_Senses_SenseId");
            RenameObject(migrationBuilder, "PK_LessonVocabularyWords", "PK_LessonSenses");
            RenameObject(migrationBuilder, "FK_LessonVocabularyWords_Lessons_LessonId", "FK_LessonSenses_Lessons_LessonId");
            RenameObject(migrationBuilder, "FK_LessonVocabularyWords_VocabularyWords_VocabularyWordId", "FK_LessonSenses_Senses_SenseId");

            migrationBuilder.RenameTable(name: "VocabularyWords", newName: "Senses");
            migrationBuilder.RenameTable(name: "UserVocabularyWords", newName: "LearnerWords");
            migrationBuilder.RenameTable(name: "LessonVocabularyWords", newName: "LessonSenses");

            migrationBuilder.RenameColumn(name: "VocabularyWordId", table: "LearnerWords", newName: "SenseId");
            migrationBuilder.RenameColumn(name: "VocabularyWordId", table: "LessonSenses", newName: "SenseId");

            migrationBuilder.RenameIndex(name: "IX_VocabularyWords_AudioAssetId", table: "Senses", newName: "IX_Senses_AudioAssetId");
            migrationBuilder.RenameIndex(name: "IX_VocabularyWords_ContentHash", table: "Senses", newName: "IX_Senses_ContentHash");
            migrationBuilder.RenameIndex(name: "IX_VocabularyWords_OwnerUserId", table: "Senses", newName: "IX_Senses_OwnerUserId");
            migrationBuilder.RenameIndex(name: "IX_VocabularyWords_ShareStatus", table: "Senses", newName: "IX_Senses_ShareStatus");
            migrationBuilder.RenameIndex(
                name: "UX_VocabularyWords_ContentHash_OwnerUserId_Learner",
                table: "Senses",
                newName: "UX_Senses_ContentHash_OwnerUserId_Learner");
            migrationBuilder.RenameIndex(
                name: "IX_UserVocabularyWords_UserId_VocabularyWordId",
                table: "LearnerWords",
                newName: "IX_LearnerWords_UserId_SenseId");
            migrationBuilder.RenameIndex(name: "IX_UserVocabularyWords_VocabularyWordId", table: "LearnerWords", newName: "IX_LearnerWords_SenseId");
            migrationBuilder.RenameIndex(name: "IX_LessonVocabularyWords_VocabularyWordId", table: "LessonSenses", newName: "IX_LessonSenses_SenseId");

            // 6. NormalizedWord now lives on Lexemes.NormalizedLemma.
            migrationBuilder.DropIndex(name: "IX_VocabularyWords_NormalizedWord", table: "Senses");
            migrationBuilder.DropColumn(name: "NormalizedWord", table: "Senses");

            // 7. New learner-link fields.
            migrationBuilder.AddColumn<string>(
                name: "AddedBy",
                table: "LearnerWords",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Learner");

            migrationBuilder.AddColumn<string>(
                name: "PersonalContext",
                table: "LearnerWords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // 8. Translations.
            migrationBuilder.CreateTable(
                name: "SenseTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SenseTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SenseTranslations_Senses_SenseId",
                        column: x => x.SenseId,
                        principalTable: "Senses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_SenseTranslations_SenseId_Locale",
                table: "SenseTranslations",
                columns: new[] { "SenseId", "Locale" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 8. Translations (lossy).
            migrationBuilder.DropTable(name: "SenseTranslations");

            // 7. Learner-link fields (PersonalContext is lossy).
            migrationBuilder.DropColumn(name: "PersonalContext", table: "LearnerWords");
            migrationBuilder.DropColumn(name: "AddedBy", table: "LearnerWords");

            // 6. NormalizedWord back, recomputed from Word.
            migrationBuilder.AddColumn<string>(
                name: "NormalizedWord",
                table: "Senses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql("UPDATE [Senses] SET [NormalizedWord] = UPPER(LTRIM(RTRIM([Word])));");
            migrationBuilder.Sql("ALTER TABLE [Senses] ALTER COLUMN [NormalizedWord] nvarchar(200) NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyWords_NormalizedWord",
                table: "Senses",
                column: "NormalizedWord");

            // 5. Rename everything back.
            migrationBuilder.RenameIndex(name: "IX_LessonSenses_SenseId", table: "LessonSenses", newName: "IX_LessonVocabularyWords_VocabularyWordId");
            migrationBuilder.RenameIndex(name: "IX_LearnerWords_SenseId", table: "LearnerWords", newName: "IX_UserVocabularyWords_VocabularyWordId");
            migrationBuilder.RenameIndex(
                name: "IX_LearnerWords_UserId_SenseId",
                table: "LearnerWords",
                newName: "IX_UserVocabularyWords_UserId_VocabularyWordId");
            migrationBuilder.RenameIndex(
                name: "UX_Senses_ContentHash_OwnerUserId_Learner",
                table: "Senses",
                newName: "UX_VocabularyWords_ContentHash_OwnerUserId_Learner");
            migrationBuilder.RenameIndex(name: "IX_Senses_ShareStatus", table: "Senses", newName: "IX_VocabularyWords_ShareStatus");
            migrationBuilder.RenameIndex(name: "IX_Senses_OwnerUserId", table: "Senses", newName: "IX_VocabularyWords_OwnerUserId");
            migrationBuilder.RenameIndex(name: "IX_Senses_ContentHash", table: "Senses", newName: "IX_VocabularyWords_ContentHash");
            migrationBuilder.RenameIndex(name: "IX_Senses_AudioAssetId", table: "Senses", newName: "IX_VocabularyWords_AudioAssetId");

            migrationBuilder.RenameColumn(name: "SenseId", table: "LessonSenses", newName: "VocabularyWordId");
            migrationBuilder.RenameColumn(name: "SenseId", table: "LearnerWords", newName: "VocabularyWordId");

            migrationBuilder.RenameTable(name: "LessonSenses", newName: "LessonVocabularyWords");
            migrationBuilder.RenameTable(name: "LearnerWords", newName: "UserVocabularyWords");
            migrationBuilder.RenameTable(name: "Senses", newName: "VocabularyWords");

            RenameObject(migrationBuilder, "FK_LessonSenses_Senses_SenseId", "FK_LessonVocabularyWords_VocabularyWords_VocabularyWordId");
            RenameObject(migrationBuilder, "FK_LessonSenses_Lessons_LessonId", "FK_LessonVocabularyWords_Lessons_LessonId");
            RenameObject(migrationBuilder, "PK_LessonSenses", "PK_LessonVocabularyWords");
            RenameObject(migrationBuilder, "FK_LearnerWords_Senses_SenseId", "FK_UserVocabularyWords_VocabularyWords_VocabularyWordId");
            RenameObject(migrationBuilder, "PK_LearnerWords", "PK_UserVocabularyWords");
            RenameObject(migrationBuilder, "FK_Senses_MediaAssets_AudioAssetId", "FK_VocabularyWords_MediaAssets_AudioAssetId");
            RenameObject(migrationBuilder, "PK_Senses", "PK_VocabularyWords");

            // 4 + 3. LexemeId, its FK and index (lossy for lexeme data).
            migrationBuilder.DropForeignKey(name: "FK_Senses_Lexemes_LexemeId", table: "VocabularyWords");
            migrationBuilder.DropIndex(name: "IX_Senses_LexemeId", table: "VocabularyWords");
            migrationBuilder.DropColumn(name: "LexemeId", table: "VocabularyWords");

            // 2 + 1. Lexemes.
            migrationBuilder.DropTable(name: "Lexemes");
        }

        /// <summary>Renames a PK/FK constraint in place. <c>sp_rename</c> touches metadata only; a PK
        /// rename also renames its clustered index, with no rebuild.</summary>
        private static void RenameObject(MigrationBuilder migrationBuilder, string oldName, string newName) =>
            migrationBuilder.Sql($"EXEC sp_rename N'{oldName}', N'{newName}', N'OBJECT';");
    }
}
