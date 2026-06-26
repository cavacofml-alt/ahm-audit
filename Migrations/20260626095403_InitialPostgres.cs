using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AHM.Audit.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditoriaArchives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ArchiveYear = table.Column<int>(type: "integer", nullable: false),
                    Agent = table.Column<string>(type: "text", nullable: false),
                    AhmOfficer = table.Column<string>(type: "text", nullable: false),
                    Ticket = table.Column<string>(type: "text", nullable: false),
                    Airline = table.Column<string>(type: "text", nullable: false),
                    Aircraft = table.Column<string>(type: "text", nullable: false),
                    Registration = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RevisionUpdates = table.Column<string>(type: "text", nullable: false),
                    CorrectionTicket = table.Column<string>(type: "text", nullable: false),
                    ReasonForRecertification = table.Column<string>(type: "text", nullable: false),
                    CorrectionsMade = table.Column<string>(type: "text", nullable: false),
                    AircraftRecertified = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    B1 = table.Column<string>(type: "text", nullable: false),
                    B2 = table.Column<string>(type: "text", nullable: false),
                    B3 = table.Column<string>(type: "text", nullable: false),
                    C1 = table.Column<string>(type: "text", nullable: false),
                    C2 = table.Column<string>(type: "text", nullable: false),
                    C2_3 = table.Column<string>(type: "text", nullable: false),
                    C3 = table.Column<string>(type: "text", nullable: false),
                    C4_TakeOff = table.Column<string>(type: "text", nullable: false),
                    C4_ZeroFuel = table.Column<string>(type: "text", nullable: false),
                    C4_Landing = table.Column<string>(type: "text", nullable: false),
                    C4_Inflight = table.Column<string>(type: "text", nullable: false),
                    C4_IdealTrim = table.Column<string>(type: "text", nullable: false),
                    C5 = table.Column<string>(type: "text", nullable: false),
                    C7_1 = table.Column<string>(type: "text", nullable: false),
                    D1 = table.Column<string>(type: "text", nullable: false),
                    D2 = table.Column<string>(type: "text", nullable: false),
                    D3 = table.Column<string>(type: "text", nullable: false),
                    D5_1 = table.Column<string>(type: "text", nullable: false),
                    D5_2 = table.Column<string>(type: "text", nullable: false),
                    D6_2 = table.Column<string>(type: "text", nullable: false),
                    E1_DOW = table.Column<string>(type: "text", nullable: false),
                    E1_MRW = table.Column<string>(type: "text", nullable: false),
                    E1_MTOW = table.Column<string>(type: "text", nullable: false),
                    E1_MZFW = table.Column<string>(type: "text", nullable: false),
                    E1_MLAW = table.Column<string>(type: "text", nullable: false),
                    E2_1 = table.Column<string>(type: "text", nullable: false),
                    E2_2 = table.Column<string>(type: "text", nullable: false),
                    E3_1 = table.Column<string>(type: "text", nullable: false),
                    G1 = table.Column<string>(type: "text", nullable: false),
                    RevisionUpdate = table.Column<string>(type: "text", nullable: false),
                    LIR = table.Column<string>(type: "text", nullable: false),
                    LS = table.Column<string>(type: "text", nullable: false),
                    DatabasePrintout = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriaArchives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Auditorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    Agent = table.Column<string>(type: "text", nullable: false),
                    AhmOfficer = table.Column<string>(type: "text", nullable: false),
                    Ticket = table.Column<string>(type: "text", nullable: false),
                    Airline = table.Column<string>(type: "text", nullable: false),
                    Aircraft = table.Column<string>(type: "text", nullable: false),
                    Registration = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RevisionUpdates = table.Column<string>(type: "text", nullable: false),
                    CorrectionTicket = table.Column<string>(type: "text", nullable: false),
                    ReasonForRecertification = table.Column<string>(type: "text", nullable: false),
                    CorrectionsMade = table.Column<string>(type: "text", nullable: false),
                    AircraftRecertified = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    B1 = table.Column<string>(type: "text", nullable: false),
                    B2 = table.Column<string>(type: "text", nullable: false),
                    B3 = table.Column<string>(type: "text", nullable: false),
                    C1 = table.Column<string>(type: "text", nullable: false),
                    C2 = table.Column<string>(type: "text", nullable: false),
                    C2_3 = table.Column<string>(type: "text", nullable: false),
                    C3 = table.Column<string>(type: "text", nullable: false),
                    C4_TakeOff = table.Column<string>(type: "text", nullable: false),
                    C4_ZeroFuel = table.Column<string>(type: "text", nullable: false),
                    C4_Landing = table.Column<string>(type: "text", nullable: false),
                    C4_Inflight = table.Column<string>(type: "text", nullable: false),
                    C4_IdealTrim = table.Column<string>(type: "text", nullable: false),
                    C5 = table.Column<string>(type: "text", nullable: false),
                    C7_1 = table.Column<string>(type: "text", nullable: false),
                    D1 = table.Column<string>(type: "text", nullable: false),
                    D2 = table.Column<string>(type: "text", nullable: false),
                    D3 = table.Column<string>(type: "text", nullable: false),
                    D5_1 = table.Column<string>(type: "text", nullable: false),
                    D5_2 = table.Column<string>(type: "text", nullable: false),
                    D6_2 = table.Column<string>(type: "text", nullable: false),
                    E1_DOW = table.Column<string>(type: "text", nullable: false),
                    E1_MRW = table.Column<string>(type: "text", nullable: false),
                    E1_MTOW = table.Column<string>(type: "text", nullable: false),
                    E1_MZFW = table.Column<string>(type: "text", nullable: false),
                    E1_MLAW = table.Column<string>(type: "text", nullable: false),
                    E2_1 = table.Column<string>(type: "text", nullable: false),
                    E2_2 = table.Column<string>(type: "text", nullable: false),
                    E3_1 = table.Column<string>(type: "text", nullable: false),
                    G1 = table.Column<string>(type: "text", nullable: false),
                    RevisionUpdate = table.Column<string>(type: "text", nullable: false),
                    LIR = table.Column<string>(type: "text", nullable: false),
                    LS = table.Column<string>(type: "text", nullable: false),
                    DatabasePrintout = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditoriaArchives");

            migrationBuilder.DropTable(
                name: "Auditorias");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
