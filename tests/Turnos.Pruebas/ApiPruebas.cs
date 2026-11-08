// Turnos de policlínica — Matías Nicolás Echeveste Cardozo
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Turnos.Pruebas;

/// <summary>Levanta la API completa en memoria con una base SQLite propia y el reloj fijo.</summary>
public sealed class FabricaApi : WebApplicationFactory<Program>
{
    private readonly string nombreBase = $"Data Source=prueba-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection guardian;

    public FabricaApi()
    {
        // La base en memoria vive mientras haya una conexión abierta.
        guardian = new SqliteConnection(nombreBase);
        guardian.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Turnos", nombreBase);
        builder.ConfigureServices(s => s.Replace(ServiceDescriptor.Singleton<TimeProvider>(new FakeTimeProvider(AgendaServicioPruebas.Ahora))));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        guardian.Dispose();
    }
}

public sealed class ApiPruebas(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    private readonly HttpClient cliente = fabrica.CreateClient();
    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    private async Task<JsonElement> Json(HttpResponseMessage r) =>
        await r.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);

    [Fact]
    public async Task ValidaLosDatosDelPaciente()
    {
        var r = await cliente.PostAsJsonAsync("/api/pacientes", new { cedula = "1.234.567-8", nombre = "", nacimiento = "2030-01-01" }, Cancelacion);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var errores = (await Json(r)).GetProperty("errors");
        Assert.Equal("La cédula no es válida", errores.GetProperty("cedula")[0].GetString());
        Assert.True(errores.TryGetProperty("nombre", out _));
        Assert.True(errores.TryGetProperty("nacimiento", out _));
    }

    [Fact]
    public async Task UnCuerpoMalFormadoEsUnErrorDelCliente()
    {
        using var cuerpo = new StringContent("{\"cedula\": \"4.567.890-5\",", System.Text.Encoding.UTF8, "application/json");
        var r = await cliente.PostAsync("/api/pacientes", cuerpo, Cancelacion);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task FlujoCompletoDeUnTurno()
    {
        var alta = await cliente.PostAsJsonAsync("/api/pacientes", new { cedula = "4.567.890-5", nombre = "Valentina Ruiz", nacimiento = "1985-07-14" }, Cancelacion);
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        Assert.Equal("/api/pacientes/45678905", alta.Headers.Location!.OriginalString);
        var repetido = await cliente.PostAsJsonAsync("/api/pacientes", new { cedula = "45678905", nombre = "Otra", nacimiento = "1985-07-14" }, Cancelacion);
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);

        var ginecologa = (await cliente.GetFromJsonAsync<JsonElement>("/api/profesionales?especialidad=Ginecologia", Cancelacion))[0];
        var id = ginecologa.GetProperty("id").GetInt32();
        var libres = await cliente.GetFromJsonAsync<DateTime[]>($"/api/profesionales/{id}/disponibles?fecha=2026-12-02", Cancelacion);
        Assert.Equal(12, libres!.Length); // miércoles de 13 a 17

        var turno = await cliente.PostAsJsonAsync("/api/turnos", new { profesionalId = id, cedula = "4.567.890-5", inicio = libres[3] }, Cancelacion);
        Assert.Equal(HttpStatusCode.Created, turno.StatusCode);
        var creado = await Json(turno);
        Assert.Equal("2026-12-02T14:20:00", creado.GetProperty("fin").GetString());
        Assert.Equal("Agendado", creado.GetProperty("estado").GetString());

        var propios = await cliente.GetFromJsonAsync<JsonElement>("/api/pacientes/4.567.890-5/turnos", Cancelacion);
        Assert.Equal(1, propios.GetArrayLength());

        var baja = await cliente.DeleteAsync(turno.Headers.Location, Cancelacion);
        Assert.Equal("Cancelado", (await Json(baja)).GetProperty("estado").GetString());
    }

    [Fact]
    public async Task DevuelveProblemDetailsEnLosErroresDeNegocio()
    {
        var r = await cliente.PostAsJsonAsync("/api/turnos", new { profesionalId = 1, cedula = "99999999", inicio = "2026-12-02T09:00:00" }, Cancelacion);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType!.MediaType);
        Assert.Equal("Paciente no registrado", (await Json(r)).GetProperty("detail").GetString());

        Assert.Equal(HttpStatusCode.NotFound, (await cliente.DeleteAsync("/api/turnos/4242", Cancelacion)).StatusCode);
    }

    [Fact]
    public async Task PublicaSaludYDocumentacionOpenApi()
    {
        Assert.Equal("Healthy", await cliente.GetStringAsync("/salud", Cancelacion));
        var documento = await cliente.GetFromJsonAsync<JsonElement>("/openapi/v1.json", Cancelacion);
        Assert.True(documento.GetProperty("paths").TryGetProperty("/api/turnos", out _));
        var profesionales = await cliente.GetFromJsonAsync<JsonElement>("/api/profesionales", Cancelacion);
        Assert.Equal(4, profesionales.GetArrayLength());
    }
}
