using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddCategoryName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Categories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_PageId_Name",
                table: "Categories",
                columns: new[] { "PageId", "Name" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_PageId_Name",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Categories");
        }
    }
}
