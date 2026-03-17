using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class m1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonalCalendar_Users_UserId",
                table: "PersonalCalendar");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonalCalendar",
                table: "PersonalCalendar");

            migrationBuilder.RenameTable(
                name: "PersonalCalendar",
                newName: "PersonalCalendars");

            migrationBuilder.RenameIndex(
                name: "IX_PersonalCalendar_UserId",
                table: "PersonalCalendars",
                newName: "IX_PersonalCalendars_UserId");

            migrationBuilder.AddColumn<int>(
                name: "PersonalStartSchCalendarId",
                table: "Events",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "AesEncryptedUrl",
                table: "PersonalCalendars",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "AesNonce",
                table: "PersonalCalendars",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "AesTag",
                table: "PersonalCalendars",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "PersonalCalendars",
                type: "TEXT",
                maxLength: 34,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonalCalendars",
                table: "PersonalCalendars",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "PersonalCalendarExports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalCalendarExports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalCalendarExports_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_PersonalStartSchCalendarId",
                table: "Events",
                column: "PersonalStartSchCalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalCalendarExports_UserId",
                table: "PersonalCalendarExports",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_PersonalCalendars_PersonalStartSchCalendarId",
                table: "Events",
                column: "PersonalStartSchCalendarId",
                principalTable: "PersonalCalendars",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalCalendars_Users_UserId",
                table: "PersonalCalendars",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_PersonalCalendars_PersonalStartSchCalendarId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalCalendars_Users_UserId",
                table: "PersonalCalendars");

            migrationBuilder.DropTable(
                name: "PersonalCalendarExports");

            migrationBuilder.DropIndex(
                name: "IX_Events_PersonalStartSchCalendarId",
                table: "Events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonalCalendars",
                table: "PersonalCalendars");

            migrationBuilder.DropColumn(
                name: "PersonalStartSchCalendarId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "AesEncryptedUrl",
                table: "PersonalCalendars");

            migrationBuilder.DropColumn(
                name: "AesNonce",
                table: "PersonalCalendars");

            migrationBuilder.DropColumn(
                name: "AesTag",
                table: "PersonalCalendars");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "PersonalCalendars");

            migrationBuilder.RenameTable(
                name: "PersonalCalendars",
                newName: "PersonalCalendar");

            migrationBuilder.RenameIndex(
                name: "IX_PersonalCalendars_UserId",
                table: "PersonalCalendar",
                newName: "IX_PersonalCalendar_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonalCalendar",
                table: "PersonalCalendar",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalCalendar_Users_UserId",
                table: "PersonalCalendar",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
