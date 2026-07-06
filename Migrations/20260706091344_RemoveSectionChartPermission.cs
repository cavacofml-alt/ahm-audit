using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AHM.Audit.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSectionChartPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanViewSectionChart",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanViewSectionChart",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
