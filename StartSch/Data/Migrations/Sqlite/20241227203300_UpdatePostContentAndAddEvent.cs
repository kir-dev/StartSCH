using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class UpdatePostContentAndAddEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Body",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Excerpt",
                table: "Posts");

            migrationBuilder.AddColumn<string>(
                name: "ContentMarkdown",
                table: "Posts",
                type: "TEXT",
                maxLength: 50000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExcerptMarkdown",
                table: "Posts",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Url",
                table: "Posts",
                column: "Url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_Url",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ContentMarkdown",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ExcerptMarkdown",
                table: "Posts");

            migrationBuilder.AddColumn<string>(
                name: "Body",
                table: "Posts",
                type: "TEXT",
                maxLength: 20000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Excerpt",
                table: "Posts",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }
    }
}
