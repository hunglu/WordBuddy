using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBuddy.Content.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSenseImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ImageAssetId",
                table: "Senses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Senses_ImageAssetId",
                table: "Senses",
                column: "ImageAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Senses_MediaAssets_ImageAssetId",
                table: "Senses",
                column: "ImageAssetId",
                principalTable: "MediaAssets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Senses_MediaAssets_ImageAssetId",
                table: "Senses");

            migrationBuilder.DropIndex(
                name: "IX_Senses_ImageAssetId",
                table: "Senses");

            migrationBuilder.DropColumn(
                name: "ImageAssetId",
                table: "Senses");
        }
    }
}
