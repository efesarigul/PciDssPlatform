using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PciDss.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AiChecklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiChecks",
                table: "Policies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiChecks",
                table: "Policies");
        }
    }
}
