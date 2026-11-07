// Turnos de policlínica — Matías Nicolás Echeveste Cardozo
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Turnos.Api.Datos;
using Turnos.Api.Dominio;
using Turnos.Api.Servicios;

namespace Turnos.Pruebas;

/// <summary>Reglas de agenda sobre una base SQLite en memoria con las migraciones reales.</summary>
public sealed class AgendaServicioPruebas : IDisposable
{
    // Martes 1 de diciembre de 2026, 08:00 en Montevideo (UTC-3).
    public static readonly DateTimeOffset Ahora = new(2026, 12, 1, 8, 0, 0, TimeSpan.FromHours(-3));
    private static readonly DateOnly Martes = new(2026, 12, 1);
    private const int Pediatra = 2, Generalista = 1;

    private readonly SqliteConnection conexion = new("Data Source=:memory:");
    private readonly FakeTimeProvider reloj = new(Ahora);
    private readonly TurnosContexto bd;
    private readonly AgendaServicio agenda;

    public AgendaServicioPruebas()
    {
        conexion.Open();
        bd = new TurnosContexto(new DbContextOptionsBuilder<TurnosContexto>().UseSqlite(conexion).Options);
        bd.Database.Migrate();
        bd.Pacientes.AddRange(
            new Paciente { Cedula = "12345672", Nombre = "Lucía Pereira", Nacimiento = new DateOnly(1990, 5, 10) },
            new Paciente { Cedula = "11111111", Nombre = "Tomás Pereira", Nacimiento = new DateOnly(2018, 3, 2) },
            new Paciente { Cedula = "01111113", Nombre = "Mateo Gómez", Nacimiento = new DateOnly(2020, 8, 20) });
        bd.SaveChanges();
        agenda = new AgendaServicio(bd, reloj);
    }

    public void Dispose()
    {
        bd.Dispose();
        conexion.Dispose();
    }

    private static DateTime El(int dia, int hora, int minuto = 0) => new(2026, 12, dia, hora, minuto, 0);

    [Fact]
    public async Task OfreceLosHorariosDeLaAgendaSemanal()
    {
        // El pediatra atiende los martes de 9 a 13: 12 turnos de 20 minutos.
        var libres = (await agenda.Disponibles(Pediatra, Martes)).Valor!;
        Assert.Equal(12, libres.Count);
        Assert.Equal(El(1, 9), libres[0]);
        Assert.Equal(El(1, 12, 40), libres[^1]);
        // El generalista no atiende los martes.
        Assert.Empty((await agenda.Disponibles(Generalista, Martes)).Valor!);
        Assert.Equal(Falla.NoEncontrado, (await agenda.Disponibles(99, Martes)).Falla);
    }

    [Fact]
    public async Task AgendaYQuitaElHorarioDeLosDisponibles()
    {
        var r = await agenda.Agendar(Pediatra, "1.111.111-1", El(1, 9, 20));
        Assert.Null(r.Falla);
        Assert.Equal("Dr. Martín Silva", r.Valor!.Profesional.Nombre);
        var libres = (await agenda.Disponibles(Pediatra, Martes)).Valor!;
        Assert.DoesNotContain(El(1, 9, 20), libres);
        Assert.Equal(11, libres.Count);
    }

    [Fact]
    public async Task ElIndiceUnicoImpideDosTurnosEnElMismoHorario()
    {
        Assert.Null((await agenda.Agendar(Pediatra, "11111111", El(1, 10))).Falla);
        var otro = await agenda.Agendar(Pediatra, "01111113", El(1, 10));
        Assert.Equal(Falla.Conflicto, otro.Falla);
        Assert.Equal("El horario ya fue tomado", otro.Mensaje);
    }

    [Fact]
    public async Task UnPacienteNoRepiteEspecialidadElMismoDia()
    {
        Assert.Null((await agenda.Agendar(Pediatra, "11111111", El(1, 10))).Falla);
        Assert.Equal(Falla.Conflicto, (await agenda.Agendar(Pediatra, "11111111", El(1, 11))).Falla);
        // Otro día sí puede.
        Assert.Null((await agenda.Agendar(Pediatra, "11111111", El(3, 14))).Falla);
    }

    [Theory]
    [InlineData(1, 7, 40, "11111111", "No se puede agendar en el pasado")]
    [InlineData(1, 13, 0, "11111111", "El profesional no atiende en ese horario")]
    [InlineData(1, 9, 10, "11111111", "El profesional no atiende en ese horario")]
    [InlineData(1, 9, 0, "12345672", "Pediatría atiende a menores de 15 años")]
    public async Task RechazaPedidosFueraDeLasReglas(int dia, int hora, int minuto, string cedula, string mensaje)
    {
        var r = await agenda.Agendar(Pediatra, cedula, El(dia, hora, minuto));
        Assert.Equal(Falla.Invalido, r.Falla);
        Assert.Equal(mensaje, r.Mensaje);
    }

    [Fact]
    public async Task CancelaConAnticipacionYLiberaElHorario()
    {
        var turno = (await agenda.Agendar(Pediatra, "11111111", El(1, 12))).Valor!;
        Assert.Equal(EstadoTurno.Cancelado, (await agenda.Cancelar(turno.Id)).Valor!.Estado);
        Assert.Equal(Falla.Conflicto, (await agenda.Cancelar(turno.Id)).Falla);
        // El índice parcial ignora los cancelados: otro paciente puede tomar el horario.
        Assert.Null((await agenda.Agendar(Pediatra, "01111113", El(1, 12))).Falla);
    }

    [Fact]
    public async Task NoCancelaConMenosDeDosHoras()
    {
        var turno = (await agenda.Agendar(Pediatra, "11111111", El(1, 10, 40))).Valor!;
        reloj.Advance(TimeSpan.FromMinutes(41)); // 08:41: faltan 1 h 59 min
        var r = await agenda.Cancelar(turno.Id);
        Assert.Equal(Falla.Conflicto, r.Falla);
        Assert.Equal(Falla.NoEncontrado, (await agenda.Cancelar(999)).Falla);
    }
}
