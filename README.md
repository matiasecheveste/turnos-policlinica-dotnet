# Turnos de policlínica — .NET 10

API para agendar turnos médicos en una policlínica, con **ASP.NET Core 10 (Minimal API)**, Entity Framework Core 10 con migraciones y pruebas xUnit v3, tanto unitarias como de integración sobre `WebApplicationFactory`.

## Reglas de negocio

- Cada profesional tiene una agenda semanal, por ejemplo "miércoles de 13 a 17", y los turnos duran 20 minutos.
- Pacientes:
  - se identifican con la **cédula uruguaya**, validando su dígito verificador;
  - aceptan formatos como `1.234.567-2` o `12345672`;
  - no pueden tener dos turnos de la misma especialidad el mismo día.
- Pediatría atiende sólo a menores de 15 años, calculando la edad en la fecha del turno.
- No se agenda en el pasado. Se cancela con al menos 2 horas de anticipación, y el horario cancelado vuelve a estar disponible.

## Decisiones técnicas

- **Índice único parcial** `(ProfesionalId, Inicio) WHERE Estado <> 'Cancelado'`. Si dos pedidos simultáneos pasan la validación, la base rechaza el segundo y la API responde `409`. Los turnos cancelados no bloquean el horario.
- **`TimeProvider`**: el servicio no usa `DateTime.Now`. Las pruebas usan `FakeTimeProvider` para fijar la hora y avanzarla, por ejemplo para probar la cancelación tardía. La hora se convierte a `America/Montevideo`.
- **Resultados de negocio explícitos**: `Resultado<T>` en lugar de excepciones para los casos esperados. Los endpoints los traducen a *Problem Details* (RFC 9457) con `TypedResults`.
- **Migraciones EF Core** aplicadas al iniciar, con datos semilla (`HasData`). La CI verifica con `has-pending-model-changes` que el modelo y las migraciones no se desincronicen.
- **Rate limiting** de ventana fija por IP en las operaciones de escritura, más *health checks* de la base y documento **OpenAPI** nativo (`/openapi/v1.json`).
- `TreatWarningsAsErrors`, tipos que no admiten null y `dotnet format` verificado en CI.

## Endpoints

| Método | Ruta |
|---|---|
| GET | `/api/profesionales?especialidad=Pediatria` |
| GET | `/api/profesionales/{id}/disponibles?fecha=2026-12-02` |
| POST | `/api/pacientes` — `{ "cedula": "1.234.567-2", "nombre": "…", "nacimiento": "1990-05-10" }` |
| GET | `/api/pacientes/{cedula}/turnos` |
| POST | `/api/turnos` — `{ "profesionalId": 4, "cedula": "12345672", "inicio": "2026-12-02T13:00:00" }` |
| DELETE | `/api/turnos/{id}` |
| GET | `/salud`, `/openapi/v1.json` |

## Uso

```bash
dotnet run --project src/Turnos.Api        # http://localhost:5190
dotnet test                                # 26 pruebas
docker build -t turnos . && docker run -p 8080:8080 -v turnos:/datos turnos
```

## Estructura

```
src/Turnos.Api/
  Dominio/      entidades y validación de cédula
  Datos/        DbContext, índices, semilla y migraciones
  Servicios/    reglas de agenda (AgendaServicio)
  Endpoints/    rutas Minimal API y DTO
tests/Turnos.Pruebas/
  CedulaPruebas.cs          teorías con casos válidos e inválidos
  AgendaServicioPruebas.cs  reglas sobre SQLite en memoria con las migraciones reales
  ApiPruebas.cs             HTTP de punta a punta con WebApplicationFactory
```

## Autor

Matías Nicolás Echeveste Cardozo — licencia MIT.
