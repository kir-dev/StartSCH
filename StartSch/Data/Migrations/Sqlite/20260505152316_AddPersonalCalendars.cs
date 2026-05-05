using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddPersonalCalendars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultPersonalCalendarCategoryId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultPersonalCalendarExamCategoryId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalCalendarConfiguration",
                table: "Users",
                type: "TEXT",
                maxLength: 100000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersonalCalendarCategoryId",
                table: "Events",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PersonalCalendars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 34, nullable: false),
                    AesNonce = table.Column<byte[]>(type: "BLOB", nullable: true),
                    AesEncryptedUrl = table.Column<byte[]>(type: "BLOB", nullable: true),
                    AesTag = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Color = table.Column<uint>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalCalendars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalCalendars_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_DefaultPersonalCalendarCategoryId",
                table: "Users",
                column: "DefaultPersonalCalendarCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DefaultPersonalCalendarExamCategoryId",
                table: "Users",
                column: "DefaultPersonalCalendarExamCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_PersonalCalendarCategoryId",
                table: "Events",
                column: "PersonalCalendarCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalCalendars_UserId",
                table: "PersonalCalendars",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_PersonalCalendars_PersonalCalendarCategoryId",
                table: "Events",
                column: "PersonalCalendarCategoryId",
                principalTable: "PersonalCalendars",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_PersonalCalendars_DefaultPersonalCalendarCategoryId",
                table: "Users",
                column: "DefaultPersonalCalendarCategoryId",
                principalTable: "PersonalCalendars",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_PersonalCalendars_DefaultPersonalCalendarExamCategoryId",
                table: "Users",
                column: "DefaultPersonalCalendarExamCategoryId",
                principalTable: "PersonalCalendars",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_PersonalCalendars_PersonalCalendarCategoryId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_PersonalCalendars_DefaultPersonalCalendarCategoryId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_PersonalCalendars_DefaultPersonalCalendarExamCategoryId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "PersonalCalendars");

            migrationBuilder.DropIndex(
                name: "IX_Users_DefaultPersonalCalendarCategoryId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DefaultPersonalCalendarExamCategoryId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Events_PersonalCalendarCategoryId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "DefaultPersonalCalendarCategoryId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DefaultPersonalCalendarExamCategoryId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersonalCalendarConfiguration",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersonalCalendarCategoryId",
                table: "Events");
        }
    }
}
