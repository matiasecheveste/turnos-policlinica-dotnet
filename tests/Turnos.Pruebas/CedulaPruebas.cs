// Turnos de policlínica — Matías Nicolás Echeveste Cardozo
using Turnos.Api.Dominio;

namespace Turnos.Pruebas;

public class CedulaPruebas
{
    [Theory]
    [InlineData("1.234.567-2")]
    [InlineData("12345672")]
    [InlineData("11111111")]
    [InlineData("111.111-3")]   // 7 dígitos: se completa con un cero a la izquierda
    public void AceptaCedulasConDigitoVerificadorCorrecto(string cedula) => Assert.True(Cedula.EsValida(cedula));

    [Theory]
    [InlineData("1.234.567-8")]
    [InlineData("12345")]
    [InlineData("1234567890")]
    [InlineData("abc.def-g")]
    [InlineData("")]
    [InlineData(null)]
    public void RechazaCedulasInvalidas(string? cedula) => Assert.False(Cedula.EsValida(cedula));

    [Fact]
    public void NormalizaQuitandoSeparadores() => Assert.Equal("01111113", Cedula.Normalizar(" 111.111-3 "));

    [Fact]
    public void CalculaLaEdadConsiderandoElCumpleanos()
    {
        var paciente = new Paciente { Cedula = "11111111", Nombre = "Tomás", Nacimiento = new DateOnly(2011, 12, 2) };
        Assert.Equal(14, paciente.EdadAl(new DateOnly(2026, 12, 1)));
        Assert.Equal(15, paciente.EdadAl(new DateOnly(2026, 12, 2)));
    }
}
