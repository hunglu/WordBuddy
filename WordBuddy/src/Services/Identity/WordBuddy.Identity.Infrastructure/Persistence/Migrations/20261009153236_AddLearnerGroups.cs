using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBuddy.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearnerGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearnerGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearnerGroupMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemovedReason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerGroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnerGroupMembers_LearnerGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "LearnerGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearnerGroupMembers_GroupId_LearnerId_Open",
                table: "LearnerGroupMembers",
                columns: new[] { "GroupId", "LearnerId" },
                unique: true,
                filter: "[Status] <> 'Removed'");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerGroupMembers_LearnerId",
                table: "LearnerGroupMembers",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerGroups_OwnerId",
                table: "LearnerGroups",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearnerGroupMembers");

            migrationBuilder.DropTable(
                name: "LearnerGroups");
        }
    }
}
