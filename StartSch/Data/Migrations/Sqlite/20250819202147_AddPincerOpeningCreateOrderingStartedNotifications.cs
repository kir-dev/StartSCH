using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddPincerOpeningCreateOrderingStartedNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BackgroundTasks_PincerOpeningId",
                table: "BackgroundTasks");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundTasks_PincerOpeningId",
                table: "BackgroundTasks",
                column: "PincerOpeningId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BackgroundTasks_PincerOpeningId",
                table: "BackgroundTasks");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundTasks_PincerOpeningId",
                table: "BackgroundTasks",
                column: "PincerOpeningId");
        }
    }
}
