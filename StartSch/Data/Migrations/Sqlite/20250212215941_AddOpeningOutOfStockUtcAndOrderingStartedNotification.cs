using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddOpeningOutOfStockUtcAndOrderingStartedNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Groups coming from Pincer are now found by PincerId or matched using the PekName. Previous schemas didn't
            // have a PincerId, make sure those have a PekName.
            migrationBuilder.Sql(
                """
                 UPDATE "Groups" SET "PekName" = "PincerName" WHERE "PekName" IS NULL;
                 """);

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Events_EventId",
                table: "Notifications");

            migrationBuilder.RenameColumn(
                name: "EventId",
                table: "Notifications",
                newName: "OpeningId");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_EventId",
                table: "Notifications",
                newName: "IX_Notifications_OpeningId");

            migrationBuilder.AddColumn<DateTime>(
                name: "OutOfStockUtc",
                table: "Events",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Events_OpeningId",
                table: "Notifications",
                column: "OpeningId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Events_OpeningId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "OutOfStockUtc",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "OpeningId",
                table: "Notifications",
                newName: "EventId");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_OpeningId",
                table: "Notifications",
                newName: "IX_Notifications_EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Events_EventId",
                table: "Notifications",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
