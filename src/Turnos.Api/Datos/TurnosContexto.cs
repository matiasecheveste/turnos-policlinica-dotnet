// Turnos de policlínica — Matías Nicolás Echeveste Cardozo
using Microsoft.EntityFrameworkCore;
using Turnos.Api.Dominio;

namespace Turnos.Api.Datos;

public class TurnosContexto(DbContextOptions<TurnosContexto> opciones) : DbContext(opciones)
{
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Profesional> Profesionales => Set<Profesional>();
    public DbSet<Turno> Turnos => Set<Turno>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Paciente>(p =>
        {
            p.Property(x => x.Cedula).HasMaxLength(8);
            p.Property(x => x.Nombre).HasMaxLength(100);
            p.HasIndex(x => x.Cedula).IsUnique();
        });

        modelo.Entity<Profesional>(p =>
        {
            p.Property(x => x.Nombre).HasMaxLength(100);
            p.Property(x => x.Especialidad).HasConversion<string>().HasMaxLength(20);
            p.HasMany(x => x.Agenda).WithOne().HasForeignKey(b => b.ProfesionalId);
        });

        modelo.Entity<Turno>(t =>
        {
            t.Property(x => x.Estado).HasConversion<string>().HasMaxLength(12);
            // Un profesional no puede tener dos turnos activos a la misma hora. El índice
            // parcial lo garantiza aun con pedidos simultáneos; los cancelados no cuentan.
            t.HasIndex(x => new { x.ProfesionalId, x.Inicio })
                .IsUnique()
                .HasFilter("\"Estado\" <> 'Cancelado'");
            t.HasIndex(x => new { x.PacienteId, x.Inicio });
        });

        Semilla(modelo);
    }

    private static void Semilla(ModelBuilder modelo)
    {
        modelo.Entity<Profesional>().HasData(
            new Profesional { Id = 1, Nombre = "Dra. Ana Rodríguez", Especialidad = Especialidad.MedicinaGeneral },
            new Profesional { Id = 2, Nombre = "Dr. Martín Silva", Especialidad = Especialidad.Pediatria },
            new Profesional { Id = 3, Nombre = "Dra. Sofía Méndez", Especialidad = Especialidad.Odontologia },
            new Profesional { Id = 4, Nombre = "Dra. Laura Fernández", Especialidad = Especialidad.Ginecologia });

        var id = 1;
        BloqueAgenda Bloque(int profesional, DayOfWeek dia, int desde, int hasta) =>
            new() { Id = id++, ProfesionalId = profesional, Dia = dia, Desde = new(desde, 0), Hasta = new(hasta, 0) };

        modelo.Entity<BloqueAgenda>().HasData(
            Bloque(1, DayOfWeek.Monday, 8, 12), Bloque(1, DayOfWeek.Wednesday, 8, 12), Bloque(1, DayOfWeek.Friday, 14, 18),
            Bloque(2, DayOfWeek.Tuesday, 9, 13), Bloque(2, DayOfWeek.Thursday, 14, 17),
            Bloque(3, DayOfWeek.Monday, 14, 19), Bloque(3, DayOfWeek.Thursday, 8, 12),
            Bloque(4, DayOfWeek.Wednesday, 13, 17));
    }
}
