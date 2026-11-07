// Turnos de policlínica — Matías Nicolás Echeveste Cardozo
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Turnos.Api.Datos;
using Turnos.Api.Endpoints;
using Turnos.Api.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TurnosContexto>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Turnos")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AgendaServicio>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<TurnosContexto>();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Límite de pedidos por IP para las operaciones que escriben (protección básica contra abuso).
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("escritura", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();

using (var alcance = app.Services.CreateScope())
{
    alcance.ServiceProvider.GetRequiredService<TurnosContexto>().Database.Migrate();
}

app.MapOpenApi();
app.MapHealthChecks("/salud");
app.MapGet("/", () => Results.Redirect("/openapi/v1.json")).ExcludeFromDescription();
app.MapearTurnos();

app.Run();

public partial class Program;
