using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PciDss.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SaqMappingFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 1,
                column: "Applicability",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 4,
                column: "Applicability",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 5,
                column: "Applicability",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 6,
                column: "Applicability",
                value: 58);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 7,
                column: "Applicability",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 10,
                column: "Applicability",
                value: 56);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 11,
                column: "Applicability",
                value: 195);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 12,
                column: "Applicability",
                value: 190);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 13,
                column: "Applicability",
                value: 60);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 16,
                column: "Applicability",
                value: 18);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 17,
                column: "Applicability",
                value: 146);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 21,
                column: "Applicability",
                value: 18);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 26,
                column: "Applicability",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 27,
                column: "Applicability",
                value: 62);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 28,
                column: "Applicability",
                value: 51);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 29,
                column: "Applicability",
                value: 59);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 32,
                column: "Applicability",
                value: 50);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 33,
                column: "Applicability",
                value: 26);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 42,
                column: "Applicability",
                value: 16);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 47,
                column: "Applicability",
                value: 126);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 1,
                column: "Applicability",
                value: 26);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 4,
                column: "Applicability",
                value: 18);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 5,
                column: "Applicability",
                value: 26);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 6,
                column: "Applicability",
                value: 26);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 7,
                column: "Applicability",
                value: 26);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 10,
                column: "Applicability",
                value: 26);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 11,
                column: "Applicability",
                value: 254);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 12,
                column: "Applicability",
                value: 254);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 13,
                column: "Applicability",
                value: 62);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 16,
                column: "Applicability",
                value: 58);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 17,
                column: "Applicability",
                value: 254);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 21,
                column: "Applicability",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 26,
                column: "Applicability",
                value: 50);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 27,
                column: "Applicability",
                value: 58);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 28,
                column: "Applicability",
                value: 59);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 29,
                column: "Applicability",
                value: 51);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 32,
                column: "Applicability",
                value: 18);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 33,
                column: "Applicability",
                value: 58);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 42,
                column: "Applicability",
                value: 18);

            migrationBuilder.UpdateData(
                table: "Requirements",
                keyColumn: "Id",
                keyValue: 47,
                column: "Applicability",
                value: 62);
        }
    }
}
