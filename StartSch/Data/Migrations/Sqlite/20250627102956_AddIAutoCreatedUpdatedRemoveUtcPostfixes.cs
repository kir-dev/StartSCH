using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddIAutoCreatedUpdatedRemoveUtcPostfixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PublishedUtc",
                table: "Posts",
                newName: "Published");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "Posts",
                newName: "Updated");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "NotificationRequests",
                newName: "Created");

            migrationBuilder.RenameColumn(
                name: "StartUtc",
                table: "Events",
                newName: "Start");

            migrationBuilder.RenameColumn(
                name: "OutOfStockUtc",
                table: "Events",
                newName: "OutOfStock");

            migrationBuilder.RenameColumn(
                name: "OrderingStartUtc",
                table: "Events",
                newName: "OrderingStart");

            migrationBuilder.RenameColumn(
                name: "OrderingEndUtc",
                table: "Events",
                newName: "OrderingEnd");

            migrationBuilder.RenameColumn(
                name: "EndUtc",
                table: "Events",
                newName: "End");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "Events",
                newName: "Updated");

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "Posts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "Events",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Created",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "Updated",
                table: "Posts",
                newName: "CreatedUtc");

            migrationBuilder.RenameColumn(
                name: "Published",
                table: "Posts",
                newName: "PublishedUtc");

            migrationBuilder.RenameColumn(
                name: "Created",
                table: "NotificationRequests",
                newName: "CreatedUtc");

            migrationBuilder.RenameColumn(
                name: "Updated",
                table: "Events",
                newName: "CreatedUtc");

            migrationBuilder.RenameColumn(
                name: "Start",
                table: "Events",
                newName: "StartUtc");

            migrationBuilder.RenameColumn(
                name: "OutOfStock",
                table: "Events",
                newName: "OutOfStockUtc");

            migrationBuilder.RenameColumn(
                name: "OrderingStart",
                table: "Events",
                newName: "OrderingStartUtc");

            migrationBuilder.RenameColumn(
                name: "OrderingEnd",
                table: "Events",
                newName: "OrderingEndUtc");

            migrationBuilder.RenameColumn(
                name: "End",
                table: "Events",
                newName: "EndUtc");
        }
    }
}
