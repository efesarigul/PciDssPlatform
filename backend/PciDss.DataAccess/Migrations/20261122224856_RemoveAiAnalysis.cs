using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PciDss.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAiAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiChecks",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "AiConfidence",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "AiGaps",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "AiModel",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "AiQuotes",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "AiVerdict",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "AnalyzedAt",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Policies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiChecks",
                table: "Policies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AiConfidence",
                table: "Policies",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiGaps",
                table: "Policies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiModel",
                table: "Policies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiQuotes",
                table: "Policies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "Policies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AiVerdict",
                table: "Policies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AnalyzedAt",
                table: "Policies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Policies",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
