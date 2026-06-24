using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AHM.Audit.Migrations
{
    /// <inheritdoc />
    public partial class ArchiveTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditoriaArchives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArchiveYear = table.Column<int>(type: "int", nullable: false),
                    Agent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AhmOfficer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ticket = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Airline = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aircraft = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Registration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevisionUpdates = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectionTicket = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReasonForRecertification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectionsMade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AircraftRecertified = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    B1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    B2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    B3 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    C1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    C2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    C2_3 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    C3 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    C4_TakeOff = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    C4_ZeroFuel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    C4_Landing = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    C4_Inflight = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    C4_IdealTrim = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    C5 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    C7_1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    D1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    D2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    D3 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    D5_1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    D5_2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    D6_2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    E1_DOW = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    E1_MRW = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    E1_MTOW = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    E1_MZFW = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    E1_MLAW = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    E2_1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    E2_2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    E3_1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    G1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RevisionUpdate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LIR = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LS = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DatabasePrintout = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriaArchives", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditoriaArchives");
        }
    }
}
