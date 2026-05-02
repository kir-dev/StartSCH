using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Idk2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_PersonalCalendars_PersonalStartSchCalendarId",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "PersonalStartSchCalendarId",
                table: "Events",
                newName: "PersonalCalendarCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Events_PersonalStartSchCalendarId",
                table: "Events",
                newName: "IX_Events_PersonalCalendarCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_PersonalCalendars_PersonalCalendarCategoryId",
                table: "Events",
                column: "PersonalCalendarCategoryId",
                principalTable: "PersonalCalendars",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_PersonalCalendars_PersonalCalendarCategoryId",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "PersonalCalendarCategoryId",
                table: "Events",
                newName: "PersonalStartSchCalendarId");

            migrationBuilder.RenameIndex(
                name: "IX_Events_PersonalCalendarCategoryId",
                table: "Events",
                newName: "IX_Events_PersonalStartSchCalendarId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_PersonalCalendars_PersonalStartSchCalendarId",
                table: "Events",
                column: "PersonalStartSchCalendarId",
                principalTable: "PersonalCalendars",
                principalColumn: "Id");
        }
    }
}
