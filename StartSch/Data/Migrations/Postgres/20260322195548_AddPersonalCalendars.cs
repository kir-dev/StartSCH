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
            migrationBuilder.AddColumn<string>(
                name: "PersonalCalendarConfiguration",
                table: "Users",
                type: "character varying(100000)",
                maxLength: 100000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersonalStartSchCalendarId",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PersonalCalendarExports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
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
                    AesTag = table.Column<byte[]>(type: "bytea", nullable: true)
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
                name: "IX_Events_PersonalStartSchCalendarId",
                table: "Events",
                column: "PersonalStartSchCalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalCalendarExports_UserId",
                table: "PersonalCalendarExports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalCalendars_UserId",
                table: "PersonalCalendars",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_PersonalCalendars_PersonalStartSchCalendarId",
                table: "Events",
                column: "PersonalStartSchCalendarId",
                principalTable: "PersonalCalendars",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_PersonalCalendars_PersonalStartSchCalendarId",
                table: "Events");

            migrationBuilder.DropTable(
                name: "PersonalCalendarExports");

            migrationBuilder.DropTable(
                name: "PersonalCalendars");

            migrationBuilder.DropIndex(
                name: "IX_Events_PersonalStartSchCalendarId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "PersonalCalendarConfiguration",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersonalStartSchCalendarId",
                table: "Events");
        }
    }
}
