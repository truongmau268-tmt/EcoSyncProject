using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoSyncWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddManualEndToProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsManuallyEnded",
                table: "Projects",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsManuallyEnded",
                table: "Projects");
        }
    }
}
