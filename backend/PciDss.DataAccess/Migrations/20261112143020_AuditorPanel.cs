using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PciDss.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AuditorPanel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditorAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    AuditorUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditorAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditorAccesses_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditorAccesses_Users_AuditorUserId",
                        column: x => x.AuditorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditorReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    RequirementId = table.Column<int>(type: "int", nullable: false),
                    AuditorUserId = table.Column<int>(type: "int", nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditorReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditorReviews_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditorReviews_Requirements_RequirementId",
                        column: x => x.RequirementId,
                        principalTable: "Requirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditorReviews_Users_AuditorUserId",
                        column: x => x.AuditorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditorAccesses_AuditorUserId",
                table: "AuditorAccesses",
                column: "AuditorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditorAccesses_OrganizationId_AuditorUserId",
                table: "AuditorAccesses",
                columns: new[] { "OrganizationId", "AuditorUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditorReviews_AuditorUserId",
                table: "AuditorReviews",
                column: "AuditorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditorReviews_OrganizationId_RequirementId",
                table: "AuditorReviews",
                columns: new[] { "OrganizationId", "RequirementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditorReviews_RequirementId",
                table: "AuditorReviews",
                column: "RequirementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditorAccesses");

            migrationBuilder.DropTable(
                name: "AuditorReviews");
        }
    }
}
