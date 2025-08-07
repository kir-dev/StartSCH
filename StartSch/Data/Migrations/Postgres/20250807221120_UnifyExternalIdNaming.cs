using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class UnifyExternalIdNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_EventId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_ExternalUrl",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Events_ParentId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Categories_PageId_Name",
                table: "Categories");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "Pages",
                newName: "ExternalUrl");

            migrationBuilder.RenameIndex(
                name: "IX_Pages_Url",
                table: "Pages",
                newName: "IX_Pages_ExternalUrl");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "Events",
                newName: "ExternalUrl");

            migrationBuilder.AddColumn<int>(
                name: "ExternalIdInt",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_EventId_ExternalIdInt",
                table: "Posts",
                columns: new[] { "EventId", "ExternalIdInt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_ParentId_ExternalIdInt",
                table: "Events",
                columns: new[] { "ParentId", "ExternalIdInt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_EventId_ExternalIdInt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Events_ParentId_ExternalIdInt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ExternalIdInt",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "ExternalUrl",
                table: "Pages",
                newName: "Url");

            migrationBuilder.RenameIndex(
                name: "IX_Pages_ExternalUrl",
                table: "Pages",
                newName: "IX_Pages_Url");

            migrationBuilder.RenameColumn(
                name: "ExternalUrl",
                table: "Events",
                newName: "Url");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_EventId",
                table: "Posts",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ExternalUrl",
                table: "Posts",
                column: "ExternalUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_ParentId",
                table: "Events",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_PageId_Name",
                table: "Categories",
                columns: new[] { "PageId", "Name" });
        }
    }
}
