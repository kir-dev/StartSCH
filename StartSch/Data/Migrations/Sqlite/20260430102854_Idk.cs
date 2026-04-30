using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Idk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonalCalendarExports");

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

            migrationBuilder.CreateIndex(
                name: "IX_Users_DefaultPersonalCalendarCategoryId",
                table: "Users",
                column: "DefaultPersonalCalendarCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DefaultPersonalCalendarExamCategoryId",
                table: "Users",
                column: "DefaultPersonalCalendarExamCategoryId");

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
                name: "FK_Users_PersonalCalendars_DefaultPersonalCalendarCategoryId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_PersonalCalendars_DefaultPersonalCalendarExamCategoryId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DefaultPersonalCalendarCategoryId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DefaultPersonalCalendarExamCategoryId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DefaultPersonalCalendarCategoryId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DefaultPersonalCalendarExamCategoryId",
                table: "Users");

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
                name: "IX_PersonalCalendarExports_UserId",
                table: "PersonalCalendarExports",
                column: "UserId");
        }
    }
}
