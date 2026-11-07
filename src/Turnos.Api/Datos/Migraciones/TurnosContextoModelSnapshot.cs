// Turnos de policlínica — Matías Nicolás Echeveste Cardozo
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Turnos.Api.Datos;

#nullable disable

namespace Turnos.Api.Datos.Migraciones
{
    [DbContext(typeof(TurnosContexto))]
    partial class TurnosContextoModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "10.0.14");

            modelBuilder.Entity("Turnos.Api.Dominio.BloqueAgenda", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<TimeOnly>("Desde")
                        .HasColumnType("TEXT");

                    b.Property<int>("Dia")
                        .HasColumnType("INTEGER");

                    b.Property<TimeOnly>("Hasta")
                        .HasColumnType("TEXT");

                    b.Property<int>("ProfesionalId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ProfesionalId");

                    b.ToTable("BloqueAgenda");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Desde = new TimeOnly(8, 0, 0),
                            Dia = 1,
                            Hasta = new TimeOnly(12, 0, 0),
                            ProfesionalId = 1
                        },
                        new
                        {
                            Id = 2,
                            Desde = new TimeOnly(8, 0, 0),
                            Dia = 3,
                            Hasta = new TimeOnly(12, 0, 0),
                            ProfesionalId = 1
                        },
                        new
                        {
                            Id = 3,
                            Desde = new TimeOnly(14, 0, 0),
                            Dia = 5,
                            Hasta = new TimeOnly(18, 0, 0),
                            ProfesionalId = 1
                        },
                        new
                        {
                            Id = 4,
                            Desde = new TimeOnly(9, 0, 0),
                            Dia = 2,
                            Hasta = new TimeOnly(13, 0, 0),
                            ProfesionalId = 2
                        },
                        new
                        {
                            Id = 5,
                            Desde = new TimeOnly(14, 0, 0),
                            Dia = 4,
                            Hasta = new TimeOnly(17, 0, 0),
                            ProfesionalId = 2
                        },
                        new
                        {
                            Id = 6,
                            Desde = new TimeOnly(14, 0, 0),
                            Dia = 1,
                            Hasta = new TimeOnly(19, 0, 0),
                            ProfesionalId = 3
                        },
                        new
                        {
                            Id = 7,
                            Desde = new TimeOnly(8, 0, 0),
                            Dia = 4,
                            Hasta = new TimeOnly(12, 0, 0),
                            ProfesionalId = 3
                        },
                        new
                        {
                            Id = 8,
                            Desde = new TimeOnly(13, 0, 0),
                            Dia = 3,
                            Hasta = new TimeOnly(17, 0, 0),
                            ProfesionalId = 4
                        });
                });

            modelBuilder.Entity("Turnos.Api.Dominio.Paciente", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Cedula")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("TEXT");

                    b.Property<DateOnly>("Nacimiento")
                        .HasColumnType("TEXT");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Cedula")
                        .IsUnique();

                    b.ToTable("Pacientes");
                });

            modelBuilder.Entity("Turnos.Api.Dominio.Profesional", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Especialidad")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("TEXT");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Profesionales");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Especialidad = "MedicinaGeneral",
                            Nombre = "Dra. Ana Rodríguez"
                        },
                        new
                        {
                            Id = 2,
                            Especialidad = "Pediatria",
                            Nombre = "Dr. Martín Silva"
                        },
                        new
                        {
                            Id = 3,
                            Especialidad = "Odontologia",
                            Nombre = "Dra. Sofía Méndez"
                        },
                        new
                        {
                            Id = 4,
                            Especialidad = "Ginecologia",
                            Nombre = "Dra. Laura Fernández"
                        });
                });

            modelBuilder.Entity("Turnos.Api.Dominio.Turno", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasMaxLength(12)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Inicio")
                        .HasColumnType("TEXT");

                    b.Property<int>("PacienteId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ProfesionalId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("PacienteId", "Inicio");

                    b.HasIndex("ProfesionalId", "Inicio")
                        .IsUnique()
                        .HasFilter("\"Estado\" <> 'Cancelado'");

                    b.ToTable("Turnos");
                });

            modelBuilder.Entity("Turnos.Api.Dominio.BloqueAgenda", b =>
                {
                    b.HasOne("Turnos.Api.Dominio.Profesional", null)
                        .WithMany("Agenda")
                        .HasForeignKey("ProfesionalId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Turnos.Api.Dominio.Turno", b =>
                {
                    b.HasOne("Turnos.Api.Dominio.Paciente", "Paciente")
                        .WithMany()
                        .HasForeignKey("PacienteId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Turnos.Api.Dominio.Profesional", "Profesional")
                        .WithMany()
                        .HasForeignKey("ProfesionalId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Paciente");

                    b.Navigation("Profesional");
                });

            modelBuilder.Entity("Turnos.Api.Dominio.Profesional", b =>
                {
                    b.Navigation("Agenda");
                });
#pragma warning restore 612, 618
        }
    }
}
