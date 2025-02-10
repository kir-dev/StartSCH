using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddGroupPincerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PincerId",
                table: "Groups",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_PekName",
                table: "Groups",
                column: "PekName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_PincerId",
                table: "Groups",
                column: "PincerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Groups_PekName",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_PincerId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "PincerId",
                table: "Groups");
        }
    }
}
