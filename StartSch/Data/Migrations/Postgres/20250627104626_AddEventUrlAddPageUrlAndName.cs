using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddEventUrlAddPageUrlAndName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pages_CodeIdentifier",
                table: "Pages");

            migrationBuilder.RenameColumn(
                name: "Site",
                table: "Pages",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "CodeIdentifier",
                table: "Pages",
                newName: "Name");

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Events",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pages_Url",
                table: "Pages",
                column: "Url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pages_Url",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "Pages",
                newName: "Site");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Pages",
                newName: "CodeIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_CodeIdentifier",
                table: "Pages",
                column: "CodeIdentifier",
                unique: true);
        }
    }
}
