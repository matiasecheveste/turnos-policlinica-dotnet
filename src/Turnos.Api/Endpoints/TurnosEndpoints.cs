// Turnos de policlínica — Matías Nicolás Echeveste Cardozo
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Turnos.Api.Datos;
using Turnos.Api.Dominio;
using Turnos.Api.Servicios;

namespace Turnos.Api.Endpoints;

public record NuevoPaciente(string? Cedula, string? Nombre, DateOnly? Nacimiento);
public record PacienteDto(int Id, string Cedula, string Nombre, DateOnly Nacimiento);
public record NuevoTurno(int ProfesionalId, string? Cedula, DateTime Inicio);
public record TurnoDto(int Id, string Profesional, Especialidad Especialidad, string Paciente, DateTime Inicio, DateTime Fin, EstadoTurno Estado);
public record ProfesionalDto(int Id, string Nombre, Especialidad Especialidad);

public static class TurnosEndpoints
{
    private static TurnoDto Dto(Turno t) =>
        new(t.Id, t.Profesional.Nombre, t.Profesional.Especialidad, t.Paciente.Nombre, t.Inicio, t.Inicio + AgendaServicio.Duracion, t.Estado);

    private static IResult Problema(Falla falla, string mensaje) => falla switch
    {
        Falla.NoEncontrado => TypedResults.Problem(mensaje, statusCode: StatusCodes.Status404NotFound),
        Falla.Conflicto => TypedResults.Problem(mensaje, statusCode: StatusCodes.Status409Conflict),
        _ => TypedResults.Problem(mensaje, statusCode: StatusCodes.Status422UnprocessableEntity),
    };

    public static void MapearTurnos(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/profesionales", async (Especialidad? especialidad, TurnosContexto bd) =>
            await bd.Profesionales
                .Where(p => especialidad == null || p.Especialidad == especialidad)
                .OrderBy(p => p.Nombre)
                .Select(p => new ProfesionalDto(p.Id, p.Nombre, p.Especialidad))
                .ToListAsync())
            .WithSummary("Lista los profesionales, opcionalmente por especialidad");

        api.MapGet("/profesionales/{id:int}/disponibles", async (int id, DateOnly fecha, AgendaServicio agenda) =>
        {
            var r = await agenda.Disponibles(id, fecha);
            return r.Falla is { } f ? Problema(f, r.Mensaje!) : TypedResults.Ok(r.Valor);
        }).WithSummary("Horarios libres de un profesional en una fecha");

        api.MapPost("/pacientes", async Task<Results<Created<PacienteDto>, ValidationProblem, ProblemHttpResult>> (NuevoPaciente pedido, TurnosContexto bd, AgendaServicio agenda) =>
        {
            var errores = new Dictionary<string, string[]>();
            if (!Cedula.EsValida(pedido.Cedula)) errores["cedula"] = ["La cédula no es válida"];
            if (string.IsNullOrWhiteSpace(pedido.Nombre) || pedido.Nombre.Length > 100) errores["nombre"] = ["El nombre es obligatorio (máximo 100 caracteres)"];
            if (pedido.Nacimiento is not { } nacimiento || nacimiento > DateOnly.FromDateTime(agenda.Ahora()) || nacimiento.Year < 1900)
                errores["nacimiento"] = ["La fecha de nacimiento no es válida"];
            if (errores.Count > 0) return TypedResults.ValidationProblem(errores);

            var cedula = Cedula.Normalizar(pedido.Cedula)!;
            if (await bd.Pacientes.AnyAsync(p => p.Cedula == cedula))
                return TypedResults.Problem("Ya existe un paciente con esa cédula", statusCode: StatusCodes.Status409Conflict);

            var paciente = new Paciente { Cedula = cedula, Nombre = pedido.Nombre!.Trim(), Nacimiento = pedido.Nacimiento!.Value };
            bd.Pacientes.Add(paciente);
            await bd.SaveChangesAsync();
            return TypedResults.Created($"/api/pacientes/{cedula}", new PacienteDto(paciente.Id, cedula, paciente.Nombre, paciente.Nacimiento));
        }).WithSummary("Registra un paciente validando la cédula uruguaya").RequireRateLimiting("escritura");

        api.MapGet("/pacientes/{cedula}/turnos", async (string cedula, TurnosContexto bd) =>
        {
            var normal = Cedula.Normalizar(cedula);
            var turnos = await bd.Turnos.Include(t => t.Profesional).Include(t => t.Paciente)
                .Where(t => t.Paciente.Cedula == normal)
                .OrderBy(t => t.Inicio)
                .ToListAsync();
            return turnos.Select(Dto);
        }).WithSummary("Turnos de un paciente");

        api.MapPost("/turnos", async (NuevoTurno pedido, AgendaServicio agenda) =>
        {
            var r = await agenda.Agendar(pedido.ProfesionalId, pedido.Cedula ?? "", pedido.Inicio);
            return r.Falla is { } f ? Problema(f, r.Mensaje!) : TypedResults.Created($"/api/turnos/{r.Valor!.Id}", Dto(r.Valor));
        }).WithSummary("Agenda un turno de 20 minutos").RequireRateLimiting("escritura");

        api.MapDelete("/turnos/{id:int}", async (int id, AgendaServicio agenda) =>
        {
            var r = await agenda.Cancelar(id);
            return r.Falla is { } f ? Problema(f, r.Mensaje!) : TypedResults.Ok(Dto(r.Valor!));
        }).WithSummary("Cancela un turno con al menos 2 horas de anticipación").RequireRateLimiting("escritura");
    }
}
