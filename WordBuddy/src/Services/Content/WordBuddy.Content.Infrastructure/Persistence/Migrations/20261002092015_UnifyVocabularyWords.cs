using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBuddy.Content.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Moves <c>VocabularyItems</c> (lesson words) and <c>PersonalVocabularyWords</c> (learner words)
    /// into one <c>VocabularyWords</c> table, with <c>UserVocabularyWords</c> (learner → word) and
    /// <c>LessonVocabularyWords</c> (lesson → word) link tables, deduplicating learner words.
    /// <list type="number">
    /// <item>Lesson words keep their ids (they name audio blobs), become <c>System</c> words owned by
    /// the system owner, and are never merged with each other.</item>
    /// <item>Learner words are merged within an owner on equal content hash — the survivor is the
    /// <c>Shared</c> row, else the <c>PendingReview</c> row, else the oldest.</item>
    /// <item>A surviving <c>Private</c>/<c>Rejected</c> learner word whose hash equals a system word
    /// collapses into it (first system word by id); otherwise, if it equals another owner's
    /// <c>Shared</c> word (an adopted copy), it collapses into that. The owner gets a non-author link.
    /// <c>Shared</c>/<c>PendingReview</c> words are never collapsed into another owner's or a system
    /// word, so the pool and the moderation queue keep every entry.</item>
    /// <item>Private words of different owners are never merged.</item>
    /// <item>Every merged-away id gets a <c>VocabularyWordIdRemaps</c> row (OldId → NewId) for
    /// downstream services (Progress recall stats).</item>
    /// </list>
    /// <para><b>Down is lossy.</b> It rebuilds both old tables from the new ones: merged rows come back
    /// as fresh copies with new ids (Progress stats pointing at the old ids don't follow), a system word
    /// in several lessons goes back to its first lesson only, and a copy's owner age group is taken from
    /// the user's own words (Child when unknown). Acceptable for a one-way refactor.</para>
    /// </summary>
    public partial class UnifyVocabularyWords : Migration
    {
        private const string SystemOwnerId = "00000000-0000-0000-0000-00000000c0de";

        /// <summary>SQL for <c>VocabularyWord.ComputeContentHash</c>: upper-case hex SHA-256 over the
        /// UTF-16LE (NVARCHAR) normalized Word, Definition, Example joined by NCHAR(31).</summary>
        private static string HashSql(string alias) =>
            $"CONVERT(char(64), HASHBYTES('SHA2_256', CONCAT(" +
            $"UPPER(LTRIM(RTRIM({alias}.[Word]))), NCHAR(31), " +
            $"UPPER(LTRIM(RTRIM({alias}.[Definition]))), NCHAR(31), " +
            $"UPPER(LTRIM(RTRIM(ISNULL({alias}.[Example], N'')))))), 2)";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. New tables. Indexes come after the data so the learner-uniqueness index validates it.
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

            migrationBuilder.CreateTable(
                name: "VocabularyWords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Word = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Example = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NormalizedWord = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContentHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    AudioAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerAgeGroup = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ShareStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VisibleToChildren = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModeratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModeratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyWords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabularyWords_MediaAssets_AudioAssetId",
                        column: x => x.AudioAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LessonVocabularyWords",
                columns: table => new
                {
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VocabularyWordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonVocabularyWords", x => new { x.LessonId, x.VocabularyWordId });
                    table.ForeignKey(
                        name: "FK_LessonVocabularyWords_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonVocabularyWords_VocabularyWords_VocabularyWordId",
                        column: x => x.VocabularyWordId,
                        principalTable: "VocabularyWords",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserVocabularyWords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VocabularyWordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAuthor = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVocabularyWords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserVocabularyWords_VocabularyWords_VocabularyWordId",
                        column: x => x.VocabularyWordId,
                        principalTable: "VocabularyWords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 2. System words — same ids (audio blob names), never merged; one lesson link each.
            migrationBuilder.Sql($@"
INSERT INTO [VocabularyWords]
    ([Id], [Word], [Definition], [Example], [NormalizedWord], [ContentHash], [AudioAssetId], [Source],
     [OwnerUserId], [OwnerAgeGroup], [ShareStatus], [VisibleToChildren], [CreatedAtUtc], [ModeratedAtUtc], [ModeratedByUserId])
SELECT v.[Id], v.[Word], v.[Definition], v.[Example], UPPER(LTRIM(RTRIM(v.[Word]))), {HashSql("v")}, v.[AudioAssetId], N'System',
       '{SystemOwnerId}', NULL, N'Private', 0, SYSUTCDATETIME(), NULL, NULL
FROM [VocabularyItems] v;

INSERT INTO [LessonVocabularyWords] ([LessonId], [VocabularyWordId], [SortOrder])
SELECT v.[LessonId], v.[Id], ROW_NUMBER() OVER (PARTITION BY v.[LessonId] ORDER BY v.[Id]) - 1
FROM [VocabularyItems] v;
");

            // 3–4. Learner words: merge, insert survivors, link owners, record remaps. One batch so
            // the temp table lives for all of it.
            migrationBuilder.Sql($@"
CREATE TABLE #Learner (
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [OwnerUserId] uniqueidentifier NOT NULL,
    [ContentHash] char(64) NOT NULL,
    [ShareStatus] nvarchar(20) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [GroupSurvivorId] uniqueidentifier NULL,
    [FinalId] uniqueidentifier NULL,
    [IsAuthor] bit NULL);

INSERT INTO #Learner ([Id], [OwnerUserId], [ContentHash], [ShareStatus], [CreatedAtUtc])
SELECT p.[Id], p.[OwnerUserId], {HashSql("p")}, p.[ShareStatus], p.[CreatedAtUtc]
FROM [PersonalVocabularyWords] p;

-- Same owner, same hash: Shared wins, then PendingReview, then the oldest.
WITH ranked AS (
    SELECT [Id],
           FIRST_VALUE([Id]) OVER (
               PARTITION BY [OwnerUserId], [ContentHash]
               ORDER BY CASE [ShareStatus] WHEN N'Shared' THEN 0 WHEN N'PendingReview' THEN 1 ELSE 2 END,
                        [CreatedAtUtc], [Id]) AS [SurvivorId]
    FROM #Learner)
UPDATE l SET [GroupSurvivorId] = r.[SurvivorId]
FROM #Learner l JOIN ranked r ON r.[Id] = l.[Id];

-- A Private/Rejected survivor collapses into a system word, else into another owner's Shared word.
UPDATE s
SET [FinalId] = COALESCE(sys.[Id], sh.[Id], s.[Id]),
    [IsAuthor] = CASE WHEN sys.[Id] IS NULL AND sh.[Id] IS NULL THEN 1 ELSE 0 END
FROM #Learner s
OUTER APPLY (
    SELECT TOP 1 w.[Id] FROM [VocabularyWords] w
    WHERE w.[Source] = N'System' AND w.[ContentHash] = s.[ContentHash]
      AND s.[ShareStatus] IN (N'Private', N'Rejected')
    ORDER BY w.[Id]) sys
OUTER APPLY (
    SELECT TOP 1 o.[Id] FROM #Learner o
    WHERE o.[Id] = o.[GroupSurvivorId] AND o.[ShareStatus] = N'Shared'
      AND o.[OwnerUserId] <> s.[OwnerUserId] AND o.[ContentHash] = s.[ContentHash]
      AND s.[ShareStatus] IN (N'Private', N'Rejected')
    ORDER BY o.[CreatedAtUtc], o.[Id]) sh
WHERE s.[Id] = s.[GroupSurvivorId];

UPDATE l SET [FinalId] = s.[FinalId], [IsAuthor] = s.[IsAuthor]
FROM #Learner l JOIN #Learner s ON s.[Id] = l.[GroupSurvivorId]
WHERE l.[Id] <> l.[GroupSurvivorId];

INSERT INTO [VocabularyWords]
    ([Id], [Word], [Definition], [Example], [NormalizedWord], [ContentHash], [AudioAssetId], [Source],
     [OwnerUserId], [OwnerAgeGroup], [ShareStatus], [VisibleToChildren], [CreatedAtUtc], [ModeratedAtUtc], [ModeratedByUserId])
SELECT p.[Id], p.[Word], p.[Definition], p.[Example], UPPER(LTRIM(RTRIM(p.[Word]))), l.[ContentHash], NULL, N'Learner',
       p.[OwnerUserId], p.[OwnerAgeGroup], p.[ShareStatus], p.[VisibleToChildren], p.[CreatedAtUtc], p.[ModeratedAtUtc], p.[ModeratedByUserId]
FROM [PersonalVocabularyWords] p JOIN #Learner l ON l.[Id] = p.[Id]
WHERE l.[FinalId] = l.[Id];

-- One link per owner per surviving word; adopted/system matches keep the copy's creation time.
INSERT INTO [UserVocabularyWords] ([Id], [UserId], [VocabularyWordId], [AddedAtUtc], [IsAuthor])
SELECT NEWID(), [OwnerUserId], [FinalId], MIN([CreatedAtUtc]), CAST(MAX(CAST([IsAuthor] AS int)) AS bit)
FROM #Learner
GROUP BY [OwnerUserId], [FinalId];

INSERT INTO [VocabularyWordIdRemaps] ([OldId], [NewId], [PublishedAtUtc])
SELECT [Id], [FinalId], NULL FROM #Learner WHERE [FinalId] <> [Id];

DROP TABLE #Learner;
");

            // 5. Old tables go; audio references now live on VocabularyWords.
            migrationBuilder.DropTable(
                name: "PersonalVocabularyWords");

            migrationBuilder.DropTable(
                name: "VocabularyItems");

            migrationBuilder.CreateIndex(
                name: "IX_LessonVocabularyWords_VocabularyWordId",
                table: "LessonVocabularyWords",
                column: "VocabularyWordId");

            migrationBuilder.CreateIndex(
                name: "IX_UserVocabularyWords_UserId_VocabularyWordId",
                table: "UserVocabularyWords",
                columns: new[] { "UserId", "VocabularyWordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserVocabularyWords_VocabularyWordId",
                table: "UserVocabularyWords",
                column: "VocabularyWordId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyWordIdRemaps_PublishedAtUtc",
                table: "VocabularyWordIdRemaps",
                column: "PublishedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyWords_AudioAssetId",
                table: "VocabularyWords",
                column: "AudioAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyWords_ContentHash",
                table: "VocabularyWords",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyWords_NormalizedWord",
                table: "VocabularyWords",
                column: "NormalizedWord");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyWords_OwnerUserId",
                table: "VocabularyWords",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyWords_ShareStatus",
                table: "VocabularyWords",
                column: "ShareStatus");

            migrationBuilder.CreateIndex(
                name: "UX_VocabularyWords_ContentHash_OwnerUserId_Learner",
                table: "VocabularyWords",
                columns: new[] { "ContentHash", "OwnerUserId" },
                unique: true,
                filter: "[Source] = 'Learner'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonalVocabularyWords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Example = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ModeratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModeratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OwnerAgeGroup = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShareStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VisibleToChildren = table.Column<bool>(type: "bit", nullable: false),
                    Word = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalVocabularyWords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AudioAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Definition = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Example = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Word = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
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

            migrationBuilder.Sql(@"
-- Lesson words: each system word back to its first lesson.
INSERT INTO [VocabularyItems] ([Id], [AudioAssetId], [Definition], [Example], [LessonId], [Word])
SELECT w.[Id], w.[AudioAssetId], w.[Definition], ISNULL(w.[Example], N''), x.[LessonId], w.[Word]
FROM [VocabularyWords] w
JOIN (SELECT [VocabularyWordId], [LessonId],
             ROW_NUMBER() OVER (PARTITION BY [VocabularyWordId] ORDER BY [SortOrder], [LessonId]) AS rn
      FROM [LessonVocabularyWords]) x ON x.[VocabularyWordId] = w.[Id] AND x.rn = 1
WHERE w.[Source] = N'System';

-- Learner words keep their id and owner (also those whose author has since unlinked them,
-- so the shared pool survives).
INSERT INTO [PersonalVocabularyWords]
    ([Id], [CreatedAtUtc], [Definition], [Example], [ModeratedAtUtc], [ModeratedByUserId], [OwnerAgeGroup],
     [OwnerUserId], [ShareStatus], [VisibleToChildren], [Word])
SELECT w.[Id], w.[CreatedAtUtc], w.[Definition], w.[Example], w.[ModeratedAtUtc], w.[ModeratedByUserId],
       ISNULL(w.[OwnerAgeGroup], N'Child'), w.[OwnerUserId], w.[ShareStatus], w.[VisibleToChildren], w.[Word]
FROM [VocabularyWords] w
WHERE w.[Source] = N'Learner';

-- Non-author links become private copies, as the old adopt flow made them.
INSERT INTO [PersonalVocabularyWords]
    ([Id], [CreatedAtUtc], [Definition], [Example], [ModeratedAtUtc], [ModeratedByUserId], [OwnerAgeGroup],
     [OwnerUserId], [ShareStatus], [VisibleToChildren], [Word])
SELECT NEWID(), l.[AddedAtUtc], w.[Definition], w.[Example], NULL, NULL,
       ISNULL((SELECT TOP 1 a.[OwnerAgeGroup] FROM [VocabularyWords] a
               WHERE a.[OwnerUserId] = l.[UserId] AND a.[OwnerAgeGroup] IS NOT NULL
               ORDER BY a.[CreatedAtUtc] DESC), N'Child'),
       l.[UserId], N'Private', 0, w.[Word]
FROM [UserVocabularyWords] l
JOIN [VocabularyWords] w ON w.[Id] = l.[VocabularyWordId]
WHERE l.[IsAuthor] = 0;
");

            migrationBuilder.DropTable(
                name: "LessonVocabularyWords");

            migrationBuilder.DropTable(
                name: "UserVocabularyWords");

            migrationBuilder.DropTable(
                name: "VocabularyWordIdRemaps");

            migrationBuilder.DropTable(
                name: "VocabularyWords");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalVocabularyWords_OwnerUserId",
                table: "PersonalVocabularyWords",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalVocabularyWords_ShareStatus",
                table: "PersonalVocabularyWords",
                column: "ShareStatus");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyItems_AudioAssetId",
                table: "VocabularyItems",
                column: "AudioAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyItems_LessonId",
                table: "VocabularyItems",
                column: "LessonId");
        }
    }
}
