using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StartSch.Data.Migrations.Postgres
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
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultPersonalCalendarExamCategoryId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalCalendarConfiguration",
                table: "Users",
                type: "character varying(100000)",
                maxLength: 100000,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PersonalCalendarEncryptionKeyHash",
                table: "Users",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersonalCalendarCategoryId",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PersonalCalendars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Discriminator = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    AesNonce = table.Column<byte[]>(type: "bytea", nullable: true),
                    AesEncryptedUrl = table.Column<byte[]>(type: "bytea", nullable: true),
                    AesTag = table.Column<byte[]>(type: "bytea", nullable: true),
                    Color = table.Column<long>(type: "bigint", nullable: true)
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
                name: "FK_Users_PersonalCalendars_DefaultPersonalCalendarExamCategory~",
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
                name: "FK_Users_PersonalCalendars_DefaultPersonalCalendarExamCategory~",
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
                name: "PersonalCalendarEncryptionKeyHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersonalCalendarCategoryId",
                table: "Events");
        }
    }
}
