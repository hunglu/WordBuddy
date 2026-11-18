using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WordBuddy.Content.Infrastructure.Persistence.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "DeletedLearnerGroups",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletedLearnerGroups", x => x.GroupId);
                });

            migrationBuilder.CreateTable(
                name: "GroupWordAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupWordAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxState",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Received = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiveCount = table.Column<int>(type: "integer", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Consumed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxState", x => x.Id);
                    table.UniqueConstraint("AK_InboxState_MessageId_ConsumerId", x => new { x.MessageId, x.ConsumerId });
                });

            migrationBuilder.CreateTable(
                name: "LearnerGroupMemberProjections",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerGroupMemberProjections", x => new { x.GroupId, x.LearnerId });
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetAgeGroup = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxState",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxState", x => x.OutboxId);
                });

            migrationBuilder.CreateTable(
                name: "SupportLinkProjections",
                columns: table => new
                {
                    LinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportLinkProjections", x => x.LinkId);
                });

            migrationBuilder.CreateTable(
                name: "GrammarRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Explanation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Examples = table.Column<string>(type: "text", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phrase = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Translation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AudioAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    VideoAssetId = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "Lexemes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Lemma = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedLemma = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PartOfSpeech = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IpaUk = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IpaUs = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UkAudioAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsAudioAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Syllables = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WordForms = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'[]'"),
                    CefrLevel = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    FrequencyRank = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                columns: table => new
                {
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnqueueTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Headers = table.Column<string>(type: "text", nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    InboxMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    InboxConsumerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MessageType = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DestinationAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ResponseAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FaultAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.SequenceNumber);
                    table.ForeignKey(
                        name: "FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId",
                        columns: x => new { x.InboxMessageId, x.InboxConsumerId },
                        principalTable: "InboxState",
                        principalColumns: new[] { "MessageId", "ConsumerId" });
                    table.ForeignKey(
                        name: "FK_OutboxMessage_OutboxState_OutboxId",
                        column: x => x.OutboxId,
                        principalTable: "OutboxState",
                        principalColumn: "OutboxId");
                });

            migrationBuilder.CreateTable(
                name: "Senses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Word = table.Column<string>(type: "citext", maxLength: 200, nullable: false),
                    Definition = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Example = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Origin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Manual"),
                    Examples = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'[]'"),
                    Collocations = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'[]'"),
                    Synonyms = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'[]'"),
                    Antonyms = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'[]'"),
                    TopicTags = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'[]'"),
                    RegisterNote = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ChildSuitableHint = table.Column<bool>(type: "boolean", nullable: true),
                    LexemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentHash = table.Column<string>(type: "character(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    AudioAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerAgeGroup = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ShareStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VisibleToChildren = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModeratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModeratedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Senses_Lexemes_LexemeId",
                        column: x => x.LexemeId,
                        principalTable: "Lexemes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Senses_MediaAssets_AudioAssetId",
                        column: x => x.AudioAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Senses_MediaAssets_ImageAssetId",
                        column: x => x.ImageAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LearnerWords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAuthor = table.Column<bool>(type: "boolean", nullable: false),
                    AddedBy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Learner"),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PersonalContext = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequiresChildApproval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ChildApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChildApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerWords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnerWords_Senses_SenseId",
                        column: x => x.SenseId,
                        principalTable: "Senses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonSenses",
                columns: table => new
                {
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonSenses", x => new { x.LessonId, x.SenseId });
                    table.ForeignKey(
                        name: "FK_LessonSenses_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonSenses_Senses_SenseId",
                        column: x => x.SenseId,
                        principalTable: "Senses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SenseTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
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
                name: "IX_GroupWordAssignments_GroupId",
                table: "GroupWordAssignments",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_InboxState_Delivered",
                table: "InboxState",
                column: "Delivered");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerWords_SenseId",
                table: "LearnerWords",
                column: "SenseId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerWords_UserId_SenseId",
                table: "LearnerWords",
                columns: new[] { "UserId", "SenseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonSenses_SenseId",
                table: "LessonSenses",
                column: "SenseId");

            migrationBuilder.CreateIndex(
                name: "IX_Lexemes_UkAudioAssetId",
                table: "Lexemes",
                column: "UkAudioAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Lexemes_UsAudioAssetId",
                table: "Lexemes",
                column: "UsAudioAssetId");

            migrationBuilder.CreateIndex(
                name: "UX_Lexemes_NormalizedLemma_PartOfSpeech",
                table: "Lexemes",
                columns: new[] { "NormalizedLemma", "PartOfSpeech" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                table: "OutboxMessage",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                table: "OutboxMessage",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber",
                table: "OutboxMessage",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_OutboxId_SequenceNumber",
                table: "OutboxMessage",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_Created",
                table: "OutboxState",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Senses_AudioAssetId",
                table: "Senses",
                column: "AudioAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Senses_ContentHash",
                table: "Senses",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_Senses_ImageAssetId",
                table: "Senses",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Senses_LexemeId_Origin",
                table: "Senses",
                columns: new[] { "LexemeId", "Origin" });

            migrationBuilder.CreateIndex(
                name: "IX_Senses_OwnerUserId",
                table: "Senses",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Senses_ShareStatus",
                table: "Senses",
                column: "ShareStatus");

            migrationBuilder.CreateIndex(
                name: "UX_Senses_ContentHash_OwnerUserId_Learner",
                table: "Senses",
                columns: new[] { "ContentHash", "OwnerUserId" },
                unique: true,
                filter: "\"Source\" = 'Learner'");

            migrationBuilder.CreateIndex(
                name: "UX_SenseTranslations_SenseId_Locale",
                table: "SenseTranslations",
                columns: new[] { "SenseId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportLinkProjections_LearnerId_IsActive",
                table: "SupportLinkProjections",
                columns: new[] { "LearnerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportLinkProjections_SupporterId_LearnerId",
                table: "SupportLinkProjections",
                columns: new[] { "SupporterId", "LearnerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyPhrases");

            migrationBuilder.DropTable(
                name: "DeletedLearnerGroups");

            migrationBuilder.DropTable(
                name: "GrammarRules");

            migrationBuilder.DropTable(
                name: "GroupWordAssignments");

            migrationBuilder.DropTable(
                name: "LearnerGroupMemberProjections");

            migrationBuilder.DropTable(
                name: "LearnerWords");

            migrationBuilder.DropTable(
                name: "LessonSenses");

            migrationBuilder.DropTable(
                name: "OutboxMessage");

            migrationBuilder.DropTable(
                name: "SenseTranslations");

            migrationBuilder.DropTable(
                name: "SupportLinkProjections");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "InboxState");

            migrationBuilder.DropTable(
                name: "OutboxState");

            migrationBuilder.DropTable(
                name: "Senses");

            migrationBuilder.DropTable(
                name: "Lexemes");

            migrationBuilder.DropTable(
                name: "MediaAssets");
        }
    }
}
