using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class FixCategoryIncludes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CategoryIncludes_Categories_IncludedCategoriesId",
                table: "CategoryIncludes");

            migrationBuilder.DropForeignKey(
                name: "FK_CategoryIncludes_Categories_IncluderCategoriesId",
                table: "CategoryIncludes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CategoryIncludes",
                table: "CategoryIncludes");

            migrationBuilder.DropIndex(
                name: "IX_CategoryIncludes_IncludedId",
                table: "CategoryIncludes");

            migrationBuilder.DropIndex(
                name: "IX_CategoryIncludes_IncluderCategoriesId",
                table: "CategoryIncludes");

            migrationBuilder.DropColumn(
                name: "IncludedCategoriesId",
                table: "CategoryIncludes");

            migrationBuilder.DropColumn(
                name: "IncluderCategoriesId",
                table: "CategoryIncludes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CategoryIncludes",
                table: "CategoryIncludes",
                columns: new[] { "IncludedId", "IncluderId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CategoryIncludes",
                table: "CategoryIncludes");

            migrationBuilder.AddColumn<int>(
                name: "IncludedCategoriesId",
                table: "CategoryIncludes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IncluderCategoriesId",
                table: "CategoryIncludes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CategoryIncludes",
                table: "CategoryIncludes",
                columns: new[] { "IncludedCategoriesId", "IncluderCategoriesId" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryIncludes_IncludedId",
                table: "CategoryIncludes",
                column: "IncludedId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryIncludes_IncluderCategoriesId",
                table: "CategoryIncludes",
                column: "IncluderCategoriesId");

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryIncludes_Categories_IncludedCategoriesId",
                table: "CategoryIncludes",
                column: "IncludedCategoriesId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryIncludes_Categories_IncluderCategoriesId",
                table: "CategoryIncludes",
                column: "IncluderCategoriesId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
