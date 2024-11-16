using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartSch.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddGroupDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Event_Group_GroupId",
                table: "Event");

            migrationBuilder.DropForeignKey(
                name: "FK_Event_Tags_TagId",
                table: "Event");

            migrationBuilder.DropForeignKey(
                name: "FK_Opening_Group_GroupId",
                table: "Opening");

            migrationBuilder.DropForeignKey(
                name: "FK_Opening_Tags_TagId",
                table: "Opening");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Event_EventId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Group_GroupId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Opening_OpeningId",
                table: "Posts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Opening",
                table: "Opening");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Group",
                table: "Group");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Event",
                table: "Event");

            migrationBuilder.RenameTable(
                name: "Opening",
                newName: "Openings");

            migrationBuilder.RenameTable(
                name: "Group",
                newName: "Groups");

            migrationBuilder.RenameTable(
                name: "Event",
                newName: "Events");

            migrationBuilder.RenameIndex(
                name: "IX_Opening_TagId",
                table: "Openings",
                newName: "IX_Openings_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_Opening_GroupId",
                table: "Openings",
                newName: "IX_Openings_GroupId");

            migrationBuilder.RenameIndex(
                name: "IX_Event_TagId",
                table: "Events",
                newName: "IX_Events_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_Event_GroupId",
                table: "Events",
                newName: "IX_Events_GroupId");

            migrationBuilder.AddColumn<int>(
                name: "PekId",
                table: "Groups",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PekName",
                table: "Groups",
                type: "TEXT",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PincerName",
                table: "Groups",
                type: "TEXT",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Openings",
                table: "Openings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Groups",
                table: "Groups",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Events",
                table: "Events",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_PekId",
                table: "Groups",
                column: "PekId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_PincerName",
                table: "Groups",
                column: "PincerName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Groups_GroupId",
                table: "Events",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Tags_TagId",
                table: "Events",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Openings_Groups_GroupId",
                table: "Openings",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Openings_Tags_TagId",
                table: "Openings",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Events_EventId",
                table: "Posts",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Groups_GroupId",
                table: "Posts",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Openings_OpeningId",
                table: "Posts",
                column: "OpeningId",
                principalTable: "Openings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Groups_GroupId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_Tags_TagId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Openings_Groups_GroupId",
                table: "Openings");

            migrationBuilder.DropForeignKey(
                name: "FK_Openings_Tags_TagId",
                table: "Openings");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Events_EventId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Groups_GroupId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Openings_OpeningId",
                table: "Posts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Openings",
                table: "Openings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Groups",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_PekId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_PincerName",
                table: "Groups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Events",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "PekId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "PekName",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "PincerName",
                table: "Groups");

            migrationBuilder.RenameTable(
                name: "Openings",
                newName: "Opening");

            migrationBuilder.RenameTable(
                name: "Groups",
                newName: "Group");

            migrationBuilder.RenameTable(
                name: "Events",
                newName: "Event");

            migrationBuilder.RenameIndex(
                name: "IX_Openings_TagId",
                table: "Opening",
                newName: "IX_Opening_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_Openings_GroupId",
                table: "Opening",
                newName: "IX_Opening_GroupId");

            migrationBuilder.RenameIndex(
                name: "IX_Events_TagId",
                table: "Event",
                newName: "IX_Event_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_Events_GroupId",
                table: "Event",
                newName: "IX_Event_GroupId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Opening",
                table: "Opening",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Group",
                table: "Group",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Event",
                table: "Event",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Event_Group_GroupId",
                table: "Event",
                column: "GroupId",
                principalTable: "Group",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Event_Tags_TagId",
                table: "Event",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Opening_Group_GroupId",
                table: "Opening",
                column: "GroupId",
                principalTable: "Group",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Opening_Tags_TagId",
                table: "Opening",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Event_EventId",
                table: "Posts",
                column: "EventId",
                principalTable: "Event",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Group_GroupId",
                table: "Posts",
                column: "GroupId",
                principalTable: "Group",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Opening_OpeningId",
                table: "Posts",
                column: "OpeningId",
                principalTable: "Opening",
                principalColumn: "Id");
        }
    }
}
