using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AHM.Audit.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaWB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataConclusao",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "Pontuacao",
                table: "Auditorias");

            migrationBuilder.RenameColumn(
                name: "Titulo",
                table: "Auditorias",
                newName: "Ticket");

            migrationBuilder.RenameColumn(
                name: "Responsavel",
                table: "Auditorias",
                newName: "RevisionUpdate");

            migrationBuilder.RenameColumn(
                name: "Observacoes",
                table: "Auditorias",
                newName: "Registration");

            migrationBuilder.RenameColumn(
                name: "Estado",
                table: "Auditorias",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "Descricao",
                table: "Auditorias",
                newName: "LS");

            migrationBuilder.RenameColumn(
                name: "Departamento",
                table: "Auditorias",
                newName: "LIR");

            migrationBuilder.RenameColumn(
                name: "DataCriacao",
                table: "Auditorias",
                newName: "Date");

            migrationBuilder.AddColumn<string>(
                name: "Agent",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Aircraft",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Airline",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "B1",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "B2",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "B3",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "C1",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "C2",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "C2_3",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "C3",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "C4_IdealTrim",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "C4_Inflight",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "C4_Landing",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "C4_TakeOff",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "C4_ZeroFuel",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "C5",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "C7_1",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Auditorias",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "D1",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "D2",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "D3",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "D5_1",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "D5_2",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "D6_2",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DatabasePrintout",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "E1_DOW",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "E1_MLAW",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "E1_MRW",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "E1_MTOW",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "E1_MZFW",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "E2_1",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "E2_2",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "E3_1",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "G1",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Agent",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "Aircraft",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "Airline",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "B1",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "B2",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "B3",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "C1",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "C2",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "C2_3",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "C3",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "C4_IdealTrim",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "C4_Inflight",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "C4_Landing",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "C4_TakeOff",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "C4_ZeroFuel",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "C5",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "C7_1",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "D1",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "D2",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "D3",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "D5_1",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "D5_2",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "D6_2",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "DatabasePrintout",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "E1_DOW",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "E1_MLAW",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "E1_MRW",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "E1_MTOW",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "E1_MZFW",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "E2_1",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "E2_2",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "E3_1",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "G1",
                table: "Auditorias");

            migrationBuilder.RenameColumn(
                name: "Ticket",
                table: "Auditorias",
                newName: "Titulo");

            migrationBuilder.RenameColumn(
                name: "RevisionUpdate",
                table: "Auditorias",
                newName: "Responsavel");

            migrationBuilder.RenameColumn(
                name: "Registration",
                table: "Auditorias",
                newName: "Observacoes");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Auditorias",
                newName: "Estado");

            migrationBuilder.RenameColumn(
                name: "LS",
                table: "Auditorias",
                newName: "Descricao");

            migrationBuilder.RenameColumn(
                name: "LIR",
                table: "Auditorias",
                newName: "Departamento");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Auditorias",
                newName: "DataCriacao");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataConclusao",
                table: "Auditorias",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Pontuacao",
                table: "Auditorias",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
