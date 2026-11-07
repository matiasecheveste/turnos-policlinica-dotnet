// Turnos de policlínica — Matías Nicolás Echeveste Cardozo
namespace Turnos.Api.Dominio;

public enum Especialidad
{
    MedicinaGeneral,
    Pediatria,
    Odontologia,
    Ginecologia,
}

public enum EstadoTurno
{
    Agendado,
    Atendido,
    Cancelado,
}

public class Paciente
{
    public int Id { get; set; }
    public required string Cedula { get; set; }
    public required string Nombre { get; set; }
    public DateOnly Nacimiento { get; set; }

    public int EdadAl(DateOnly fecha)
    {
        var edad = fecha.Year - Nacimiento.Year;
        return Nacimiento.AddYears(edad) > fecha ? edad - 1 : edad;
    }
}

public class Profesional
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public Especialidad Especialidad { get; set; }
    public List<BloqueAgenda> Agenda { get; set; } = [];
}

/// <summary>Horario semanal en el que atiende un profesional (por ejemplo, lunes de 08:00 a 12:00).</summary>
public class BloqueAgenda
{
    public int Id { get; set; }
    public int ProfesionalId { get; set; }
    public DayOfWeek Dia { get; set; }
    public TimeOnly Desde { get; set; }
    public TimeOnly Hasta { get; set; }
}

public class Turno
{
    public int Id { get; set; }
    public int ProfesionalId { get; set; }
    public Profesional Profesional { get; set; } = null!;
    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    public DateTime Inicio { get; set; }
    public EstadoTurno Estado { get; set; } = EstadoTurno.Agendado;
}
