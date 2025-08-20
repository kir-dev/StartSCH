using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StartSch.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddBackgroundTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmailMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FromName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FromEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentHtml = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PushNotificationMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Payload = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushNotificationMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WaitUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    EventId = table.Column<int>(type: "integer", nullable: true),
                    PincerOpeningId = table.Column<int>(type: "integer", nullable: true),
                    PostId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    MessageId = table.Column<int>(type: "integer", nullable: true),
                    SendPushNotification_UserId = table.Column<int>(type: "integer", nullable: true),
                    SendPushNotification_MessageId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackgroundTasks_EmailMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "EmailMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BackgroundTasks_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BackgroundTasks_Events_PincerOpeningId",
                        column: x => x.PincerOpeningId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BackgroundTasks_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BackgroundTasks_PushNotificationMessages_SendPushNotificati~",
                        column: x => x.SendPushNotification_MessageId,
                        principalTable: "PushNotificationMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BackgroundTasks_Users_SendPushNotification_UserId",
                        column: x => x.SendPushNotification_UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BackgroundTasks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_EventId",
                table: "Notifications",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundTasks_Discriminator_WaitUntil_Created",
                table: "BackgroundTasks",
                columns: new[] { "Discriminator", "WaitUntil", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundTasks_EventId",
                table: "BackgroundTasks",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundTasks_MessageId",
                table: "BackgroundTasks",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundTasks_PincerOpeningId",
                table: "BackgroundTasks",
                column: "PincerOpeningId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundTasks_PostId",
                table: "BackgroundTasks",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundTasks_SendPushNotification_MessageId",
                table: "BackgroundTasks",
                column: "SendPushNotification_MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundTasks_SendPushNotification_UserId",
                table: "BackgroundTasks",
                column: "SendPushNotification_UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundTasks_UserId",
                table: "BackgroundTasks",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Events_EventId",
                table: "Notifications",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Events_EventId",
                table: "Notifications");

            migrationBuilder.DropTable(
                name: "BackgroundTasks");

            migrationBuilder.DropTable(
                name: "EmailMessages");

            migrationBuilder.DropTable(
                name: "PushNotificationMessages");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_EventId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Notifications");
        }
    }
}
