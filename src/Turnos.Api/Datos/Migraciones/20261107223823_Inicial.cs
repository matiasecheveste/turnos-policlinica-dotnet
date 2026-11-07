// Turnos de policlínica — Matías Nicolás Echeveste Cardozo
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Turnos.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pacientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Cedula = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Nacimiento = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pacientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profesionales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Especialidad = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profesionales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BloqueAgenda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfesionalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dia = table.Column<int>(type: "INTEGER", nullable: false),
                    Desde = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    Hasta = table.Column<TimeOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloqueAgenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloqueAgenda_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Turnos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfesionalId = table.Column<int>(type: "INTEGER", nullable: false),
                    PacienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Inicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turnos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Turnos_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Turnos_Profesionales_ProfesionalId",
                        column: x => x.ProfesionalId,
                        principalTable: "Profesionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Profesionales",
                columns: new[] { "Id", "Especialidad", "Nombre" },
                values: new object[,]
                {
                    { 1, "MedicinaGeneral", "Dra. Ana Rodríguez" },
                    { 2, "Pediatria", "Dr. Martín Silva" },
                    { 3, "Odontologia", "Dra. Sofía Méndez" },
                    { 4, "Ginecologia", "Dra. Laura Fernández" }
                });

            migrationBuilder.InsertData(
                table: "BloqueAgenda",
                columns: new[] { "Id", "Desde", "Dia", "Hasta", "ProfesionalId" },
                values: new object[,]
                {
                    { 1, new TimeOnly(8, 0, 0), 1, new TimeOnly(12, 0, 0), 1 },
                    { 2, new TimeOnly(8, 0, 0), 3, new TimeOnly(12, 0, 0), 1 },
                    { 3, new TimeOnly(14, 0, 0), 5, new TimeOnly(18, 0, 0), 1 },
                    { 4, new TimeOnly(9, 0, 0), 2, new TimeOnly(13, 0, 0), 2 },
                    { 5, new TimeOnly(14, 0, 0), 4, new TimeOnly(17, 0, 0), 2 },
                    { 6, new TimeOnly(14, 0, 0), 1, new TimeOnly(19, 0, 0), 3 },
                    { 7, new TimeOnly(8, 0, 0), 4, new TimeOnly(12, 0, 0), 3 },
                    { 8, new TimeOnly(13, 0, 0), 3, new TimeOnly(17, 0, 0), 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BloqueAgenda_ProfesionalId",
                table: "BloqueAgenda",
                column: "ProfesionalId");

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_Cedula",
                table: "Pacientes",
                column: "Cedula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_PacienteId_Inicio",
                table: "Turnos",
                columns: new[] { "PacienteId", "Inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_ProfesionalId_Inicio",
                table: "Turnos",
                columns: new[] { "ProfesionalId", "Inicio" },
                unique: true,
                filter: "\"Estado\" <> 'Cancelado'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloqueAgenda");

            migrationBuilder.DropTable(
                name: "Turnos");

            migrationBuilder.DropTable(
                name: "Pacientes");

            migrationBuilder.DropTable(
                name: "Profesionales");
        }
    }
}
