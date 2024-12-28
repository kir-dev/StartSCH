using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Postgres
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

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Posts",
                type: "character varying(130)",
                maxLength: 130,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "ContentMarkdown",
                table: "Posts",
                type: "character varying(50000)",
                maxLength: 50000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExcerptMarkdown",
                table: "Posts",
                type: "character varying(1000)",
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

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Posts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(130)",
                oldMaxLength: 130);

            migrationBuilder.AddColumn<string>(
                name: "Body",
                table: "Posts",
                type: "character varying(20000)",
                maxLength: 20000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Excerpt",
                table: "Posts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
