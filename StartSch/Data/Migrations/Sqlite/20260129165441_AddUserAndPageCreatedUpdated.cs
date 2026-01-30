using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddUserAndPageCreatedUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Created",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "1970-01-01 00:00:00");

            migrationBuilder.AddColumn<string>(
                name: "Updated",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "1970-01-01 00:00:00");

            migrationBuilder.AddColumn<string>(
                name: "Created",
                table: "Pages",
                type: "TEXT",
                nullable: false,
                defaultValue: "1970-01-01 00:00:00");

            migrationBuilder.AddColumn<string>(
                name: "Updated",
                table: "Pages",
                type: "TEXT",
                nullable: false,
                defaultValue: "1970-01-01 00:00:00");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Created",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Updated",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Updated",
                table: "Pages");
        }
    }
}
