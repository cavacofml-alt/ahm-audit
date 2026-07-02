using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AHM.Audit.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanViewAgentChart",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewAirlineChart",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewComparativeChart",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewHeatmap",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewOfficerChart",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewQuarterProgress",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewTrend",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanViewAgentChart",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanViewAirlineChart",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanViewComparativeChart",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanViewHeatmap",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanViewOfficerChart",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanViewQuarterProgress",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanViewTrend",
                table: "Users");
        }
    }
}
