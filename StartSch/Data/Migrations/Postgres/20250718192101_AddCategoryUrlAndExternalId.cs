using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddCategoryUrlAndExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_PageId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_PageId_Name",
                table: "Categories");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "Posts",
                newName: "ExternalUrl");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_Url",
                table: "Posts",
                newName: "IX_Posts_ExternalUrl");

            migrationBuilder.AddColumn<int>(
                name: "ExternalIdInt",
                table: "Posts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExternalIdInt",
                table: "Categories",
                type: "integer",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalUrl",
                table: "Categories",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_PageId_ExternalIdInt",
                table: "Categories",
                columns: new[] { "PageId", "ExternalIdInt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_PageId_Name",
                table: "Categories",
                columns: new[] { "PageId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_PageId_ExternalIdInt",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_PageId_Name",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ExternalIdInt",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ExternalIdInt",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ExternalUrl",
                table: "Categories");

            migrationBuilder.RenameColumn(
                name: "ExternalUrl",
                table: "Posts",
                newName: "Url");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_ExternalUrl",
                table: "Posts",
                newName: "IX_Posts_Url");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_PageId",
                table: "Categories",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_PageId_Name",
                table: "Categories",
                columns: new[] { "PageId", "Name" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }
    }
}
