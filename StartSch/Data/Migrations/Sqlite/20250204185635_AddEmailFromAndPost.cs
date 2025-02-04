using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddEmailFromAndPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "From",
                table: "Emails",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PostId",
                table: "Emails",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Emails_PostId",
                table: "Emails",
                column: "PostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Emails_Posts_PostId",
                table: "Emails",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Emails_Posts_PostId",
                table: "Emails");

            migrationBuilder.DropIndex(
                name: "IX_Emails_PostId",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "From",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "PostId",
                table: "Emails");
        }
    }
}
