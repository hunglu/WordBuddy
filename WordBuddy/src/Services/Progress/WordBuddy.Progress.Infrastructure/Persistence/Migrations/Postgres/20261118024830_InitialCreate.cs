using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WordBuddy.Progress.Infrastructure.Persistence.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChallengeConsents",
                columns: table => new
                {
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnabledBy = table.Column<Guid>(type: "uuid", nullable: false),
                    EnabledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeConsents", x => new { x.ChildId, x.EnabledBy });
                });

            migrationBuilder.CreateTable(
                name: "ChallengeParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: true),
                    TargetReached = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeParticipants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgeGroup = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Alias = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AvatarId = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsGuardian = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeProfiles", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Audience = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Metric = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Target = table.Column<int>(type: "integer", nullable: true),
                    OpensAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                });

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
                name: "LearnerProgressEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    ScorePercent = table.Column<int>(type: "integer", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerProgressEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearnerWordMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastEventAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerWordMemberships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearnerWordStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Stability = table.Column<double>(type: "double precision", nullable: false),
                    Difficulty = table.Column<double>(type: "double precision", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reps = table.Column<int>(type: "integer", nullable: false),
                    Lapses = table.Column<int>(type: "integer", nullable: false),
                    FsrsPhase = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FsrsStep = table.Column<int>(type: "integer", nullable: true),
                    LastReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FirstReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerWordStates", x => x.Id);
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
                name: "ReviewLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExerciseType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Skill = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    ResponseMs = table.Column<int>(type: "integer", nullable: false),
                    HintUsed = table.Column<bool>(type: "boolean", nullable: false),
                    IsDue = table.Column<bool>(type: "boolean", nullable: false),
                    AttemptNo = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientResponseMs = table.Column<int>(type: "integer", nullable: true),
                    ServerResponseMs = table.Column<int>(type: "integer", nullable: true),
                    TimingAdjusted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewLogs", x => x.Id);
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
                name: "VocabularyExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Skill = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ExpectedAnswer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CorrectWord = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Options = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnsweredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyExercises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyLearnerSettings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewWordsPerDay = table.Column<int>(type: "integer", nullable: true),
                    SupporterNewWordCap = table.Column<int>(type: "integer", nullable: true),
                    SupporterCapSetBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyLearnerSettings", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyRecallSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WordsChecked = table.Column<int>(type: "integer", nullable: false),
                    WordsKnown = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyRecallSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyRecallStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VocabularyWordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Word = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TimesChecked = table.Column<int>(type: "integer", nullable: false),
                    TimesKnown = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastCheckedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyRecallStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VocabularySessionIssues",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedCount = table.Column<int>(type: "integer", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularySessionIssues", x => x.SessionId);
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
                name: "VocabularySessionIssueItems",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsNew = table.Column<bool>(type: "boolean", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularySessionIssueItems", x => new { x.SessionId, x.SenseId });
                    table.ForeignKey(
                        name: "FK_VocabularySessionIssueItems_VocabularySessionIssues_Session~",
                        column: x => x.SessionId,
                        principalTable: "VocabularySessionIssues",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeConsents_EnabledBy",
                table: "ChallengeConsents",
                column: "EnabledBy");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeParticipants_ChallengeId_UserId",
                table: "ChallengeParticipants",
                columns: new[] { "ChallengeId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeParticipants_UserId",
                table: "ChallengeParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_Audience_Status",
                table: "Challenges",
                columns: new[] { "Audience", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_TemplateKey_Audience_StartsAtUtc",
                table: "Challenges",
                columns: new[] { "TemplateKey", "Audience", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxState_Delivered",
                table: "InboxState",
                column: "Delivered");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerGroupMemberProjections_OwnerId",
                table: "LearnerGroupMemberProjections",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerProgressEntries_UserId_LessonId",
                table: "LearnerProgressEntries",
                columns: new[] { "UserId", "LessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearnerWordMemberships_UserId_SenseId",
                table: "LearnerWordMemberships",
                columns: new[] { "UserId", "SenseId" },
                unique: true);

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
                name: "IX_ReviewLogs_ExerciseId",
                table: "ReviewLogs",
                column: "ExerciseId",
                unique: true,
                filter: "\"ExerciseId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewLogs_UserId_OccurredAtUtc",
                table: "ReviewLogs",
                columns: new[] { "UserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewLogs_UserId_SenseId",
                table: "ReviewLogs",
                columns: new[] { "UserId", "SenseId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewLogs_UserId_SessionId_SenseId_AttemptNo",
                table: "ReviewLogs",
                columns: new[] { "UserId", "SessionId", "SenseId", "AttemptNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportLinkProjections_LearnerId_IsActive",
                table: "SupportLinkProjections",
                columns: new[] { "LearnerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportLinkProjections_SupporterId_LearnerId",
                table: "SupportLinkProjections",
                columns: new[] { "SupporterId", "LearnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyExercises_SessionId",
                table: "VocabularyExercises",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyExercises_UserId",
                table: "VocabularyExercises",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyRecallSessions_UserId_CheckedAtUtc",
                table: "VocabularyRecallSessions",
                columns: new[] { "UserId", "CheckedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyRecallStats_UserId_VocabularyWordId",
                table: "VocabularyRecallStats",
                columns: new[] { "UserId", "VocabularyWordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabularySessionIssues_UserId_IssuedAtUtc",
                table: "VocabularySessionIssues",
                columns: new[] { "UserId", "IssuedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeConsents");

            migrationBuilder.DropTable(
                name: "ChallengeParticipants");

            migrationBuilder.DropTable(
                name: "ChallengeProfiles");

            migrationBuilder.DropTable(
                name: "Challenges");

            migrationBuilder.DropTable(
                name: "DeletedLearnerGroups");

            migrationBuilder.DropTable(
                name: "LearnerGroupMemberProjections");

            migrationBuilder.DropTable(
                name: "LearnerProgressEntries");

            migrationBuilder.DropTable(
                name: "LearnerWordMemberships");

            migrationBuilder.DropTable(
                name: "LearnerWordStates");

            migrationBuilder.DropTable(
                name: "OutboxMessage");

            migrationBuilder.DropTable(
                name: "ReviewLogs");

            migrationBuilder.DropTable(
                name: "SupportLinkProjections");

            migrationBuilder.DropTable(
                name: "VocabularyExercises");

            migrationBuilder.DropTable(
                name: "VocabularyLearnerSettings");

            migrationBuilder.DropTable(
                name: "VocabularyRecallSessions");

            migrationBuilder.DropTable(
                name: "VocabularyRecallStats");

            migrationBuilder.DropTable(
                name: "VocabularySessionIssueItems");

            migrationBuilder.DropTable(
                name: "InboxState");

            migrationBuilder.DropTable(
                name: "OutboxState");

            migrationBuilder.DropTable(
                name: "VocabularySessionIssues");
        }
    }
}
