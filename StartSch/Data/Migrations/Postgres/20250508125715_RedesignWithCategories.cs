using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StartSch.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RedesignWithCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Title = table.Column<string>(type: "character varying(130)", maxLength: 130, nullable: false),
                    DescriptionMarkdown = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    OrderingStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrderingEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OutOfStockUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Events_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Events",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Pages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodeIdentifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Site = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PekId = table.Column<int>(type: "integer", nullable: true),
                    PekName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PincerId = table.Column<int>(type: "integer", nullable: true),
                    PincerName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AuthSchId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthSchEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartSchEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartSchEmailVerified = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(130)", maxLength: 130, nullable: false),
                    ExcerptMarkdown = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContentMarkdown = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: true),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PublishedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posts_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PageId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    P256DH = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Auth = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Discriminator = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    OpeningId = table.Column<int>(type: "integer", nullable: true),
                    PostId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Events_OpeningId",
                        column: x => x.OpeningId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategoryIncludes",
                columns: table => new
                {
                    IncludedCategoriesId = table.Column<int>(type: "integer", nullable: false),
                    IncluderCategoriesId = table.Column<int>(type: "integer", nullable: false),
                    IncluderId = table.Column<int>(type: "integer", nullable: false),
                    IncludedId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryIncludes", x => new { x.IncludedCategoriesId, x.IncluderCategoriesId });
                    table.ForeignKey(
                        name: "FK_CategoryIncludes_Categories_IncludedCategoriesId",
                        column: x => x.IncludedCategoriesId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryIncludes_Categories_IncludedId",
                        column: x => x.IncludedId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryIncludes_Categories_IncluderCategoriesId",
                        column: x => x.IncluderCategoriesId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryIncludes_Categories_IncluderId",
                        column: x => x.IncluderId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventCategory",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCategory", x => new { x.CategoryId, x.EventId });
                    table.ForeignKey(
                        name: "FK_EventCategory_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventCategory_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Interests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Discriminator = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    EventId = table.Column<int>(type: "integer", nullable: true),
                    OrderingStartInterest_CategoryId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Interests_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Interests_Categories_OrderingStartInterest_CategoryId",
                        column: x => x.OrderingStartInterest_CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Interests_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostCategory",
                columns: table => new
                {
                    PostId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostCategory", x => new { x.CategoryId, x.PostId });
                    table.ForeignKey(
                        name: "FK_PostCategory_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostCategory_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotificationId = table.Column<int>(type: "integer", nullable: false),
                    Discriminator = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationRequests_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterestSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    InterestId = table.Column<int>(type: "integer", nullable: false),
                    Discriminator = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterestSubscriptions_Interests_InterestId",
                        column: x => x.InterestId,
                        principalTable: "Interests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterestSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_PageId",
                table: "Categories",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryIncludes_IncludedId",
                table: "CategoryIncludes",
                column: "IncludedId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryIncludes_IncluderCategoriesId",
                table: "CategoryIncludes",
                column: "IncluderCategoriesId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryIncludes_IncluderId",
                table: "CategoryIncludes",
                column: "IncluderId");

            migrationBuilder.CreateIndex(
                name: "IX_EventCategory_EventId",
                table: "EventCategory",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ParentId",
                table: "Events",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Interests_CategoryId",
                table: "Interests",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Interests_EventId",
                table: "Interests",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Interests_OrderingStartInterest_CategoryId",
                table: "Interests",
                column: "OrderingStartInterest_CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InterestSubscriptions_InterestId",
                table: "InterestSubscriptions",
                column: "InterestId");

            migrationBuilder.CreateIndex(
                name: "IX_InterestSubscriptions_UserId",
                table: "InterestSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRequests_NotificationId",
                table: "NotificationRequests",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRequests_UserId",
                table: "NotificationRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OpeningId",
                table: "Notifications",
                column: "OpeningId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PostId",
                table: "Notifications",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_CodeIdentifier",
                table: "Pages",
                column: "CodeIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pages_PekId",
                table: "Pages",
                column: "PekId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pages_PekName",
                table: "Pages",
                column: "PekName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pages_PincerId",
                table: "Pages",
                column: "PincerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pages_PincerName",
                table: "Pages",
                column: "PincerName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostCategory_PostId",
                table: "PostCategory",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_EventId",
                table: "Posts",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Url",
                table: "Posts",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_Endpoint",
                table: "PushSubscriptions",
                column: "Endpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_UserId",
                table: "PushSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AuthSchId",
                table: "Users",
                column: "AuthSchId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryIncludes");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "EventCategory");

            migrationBuilder.DropTable(
                name: "InterestSubscriptions");

            migrationBuilder.DropTable(
                name: "NotificationRequests");

            migrationBuilder.DropTable(
                name: "PostCategory");

            migrationBuilder.DropTable(
                name: "PushSubscriptions");

            migrationBuilder.DropTable(
                name: "Interests");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "Pages");

            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
