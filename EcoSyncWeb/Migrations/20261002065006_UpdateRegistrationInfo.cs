using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoSyncWeb.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRegistrationInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserEmail",
                table: "ProjectRegistrations",
                newName: "Phone");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "ProjectRegistrations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "ProjectRegistrations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "ProjectRegistrations");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "ProjectRegistrations");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "ProjectRegistrations",
                newName: "UserEmail");
        }
    }
}
