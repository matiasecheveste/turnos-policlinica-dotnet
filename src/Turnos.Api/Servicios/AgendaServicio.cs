// Turnos de policlínica — Matías Nicolás Echeveste Cardozo
using Microsoft.EntityFrameworkCore;
using Turnos.Api.Datos;
using Turnos.Api.Dominio;

namespace Turnos.Api.Servicios;

public enum Falla
{
    NoEncontrado,
    Conflicto,
    Invalido,
}

/// <summary>Resultado de una operación de negocio: el valor o el motivo por el que no se pudo.</summary>
public readonly record struct Resultado<T>(T? Valor, Falla? Falla, string? Mensaje)
{
    public static Resultado<T> Ok(T valor) => new(valor, null, null);
    public static Resultado<T> Error(Falla falla, string mensaje) => new(default, falla, mensaje);
}

public class AgendaServicio(TurnosContexto bd, TimeProvider reloj)
{
    public static readonly TimeSpan Duracion = TimeSpan.FromMinutes(20);
    public static readonly TimeSpan AnticipacionCancelacion = TimeSpan.FromHours(2);
    public const int EdadMaximaPediatria = 15;
    public static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/Montevideo");

    public DateTime Ahora() => TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), Zona).DateTime;

    /// <summary>Horarios libres de un profesional en una fecha, según su agenda semanal.</summary>
    public async Task<Resultado<List<DateTime>>> Disponibles(int profesionalId, DateOnly fecha)
    {
        var profesional = await bd.Profesionales.Include(p => p.Agenda).FirstOrDefaultAsync(p => p.Id == profesionalId);
        if (profesional is null) return Resultado<List<DateTime>>.Error(Falla.NoEncontrado, "Profesional inexistente");

        var desde = fecha.ToDateTime(TimeOnly.MinValue);
        var hasta = desde.AddDays(1);
        var ocupados = (await bd.Turnos
                .Where(t => t.ProfesionalId == profesionalId && t.Estado != EstadoTurno.Cancelado && t.Inicio >= desde && t.Inicio < hasta)
                .Select(t => t.Inicio)
                .ToListAsync())
            .ToHashSet();

        var ahora = Ahora();
        var libres = Horarios(profesional, fecha).Where(h => h > ahora && !ocupados.Contains(h)).ToList();
        return Resultado<List<DateTime>>.Ok(libres);
    }

    public async Task<Resultado<Turno>> Agendar(int profesionalId, string cedula, DateTime inicio)
    {
        var cedulaNormal = Cedula.Normalizar(cedula);
        var paciente = cedulaNormal is null ? null : await bd.Pacientes.FirstOrDefaultAsync(p => p.Cedula == cedulaNormal);
        if (paciente is null) return Resultado<Turno>.Error(Falla.NoEncontrado, "Paciente no registrado");

        var profesional = await bd.Profesionales.Include(p => p.Agenda).FirstOrDefaultAsync(p => p.Id == profesionalId);
        if (profesional is null) return Resultado<Turno>.Error(Falla.NoEncontrado, "Profesional inexistente");

        if (inicio <= Ahora()) return Resultado<Turno>.Error(Falla.Invalido, "No se puede agendar en el pasado");
        if (!Horarios(profesional, DateOnly.FromDateTime(inicio)).Contains(inicio))
            return Resultado<Turno>.Error(Falla.Invalido, "El profesional no atiende en ese horario");
        if (profesional.Especialidad == Especialidad.Pediatria && paciente.EdadAl(DateOnly.FromDateTime(inicio)) >= EdadMaximaPediatria)
            return Resultado<Turno>.Error(Falla.Invalido, $"Pediatría atiende a menores de {EdadMaximaPediatria} años");

        var dia = inicio.Date;
        var yaTieneEseDia = await bd.Turnos.AnyAsync(t =>
            t.PacienteId == paciente.Id && t.Estado == EstadoTurno.Agendado &&
            t.Profesional.Especialidad == profesional.Especialidad &&
            t.Inicio >= dia && t.Inicio < dia.AddDays(1));
        if (yaTieneEseDia) return Resultado<Turno>.Error(Falla.Conflicto, "Ya tiene un turno de esa especialidad ese día");

        var turno = new Turno { ProfesionalId = profesional.Id, PacienteId = paciente.Id, Inicio = inicio };
        bd.Turnos.Add(turno);
        try
        {
            await bd.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Otro pedido tomó el horario entre la validación y el guardado: lo frena el índice único.
            bd.Entry(turno).State = EntityState.Detached;
            return Resultado<Turno>.Error(Falla.Conflicto, "El horario ya fue tomado");
        }
        turno.Profesional = profesional;
        turno.Paciente = paciente;
        return Resultado<Turno>.Ok(turno);
    }

    public async Task<Resultado<Turno>> Cancelar(int turnoId)
    {
        var turno = await bd.Turnos.Include(t => t.Profesional).Include(t => t.Paciente).FirstOrDefaultAsync(t => t.Id == turnoId);
        if (turno is null) return Resultado<Turno>.Error(Falla.NoEncontrado, "Turno inexistente");
        if (turno.Estado != EstadoTurno.Agendado) return Resultado<Turno>.Error(Falla.Conflicto, $"El turno está {turno.Estado.ToString().ToLowerInvariant()}");
        if (turno.Inicio - Ahora() < AnticipacionCancelacion)
            return Resultado<Turno>.Error(Falla.Conflicto, "Sólo se puede cancelar con 2 horas de anticipación");
        turno.Estado = EstadoTurno.Cancelado;
        await bd.SaveChangesAsync();
        return Resultado<Turno>.Ok(turno);
    }

    private static IEnumerable<DateTime> Horarios(Profesional profesional, DateOnly fecha)
    {
        foreach (var bloque in profesional.Agenda.Where(b => b.Dia == fecha.DayOfWeek).OrderBy(b => b.Desde))
        {
            for (var hora = bloque.Desde; hora.Add(Duracion) <= bloque.Hasta && hora >= bloque.Desde; hora = hora.Add(Duracion))
            {
                yield return fecha.ToDateTime(hora);
            }
        }
    }
}
