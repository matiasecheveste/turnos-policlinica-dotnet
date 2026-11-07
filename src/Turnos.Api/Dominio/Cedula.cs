// Turnos de policlínica — Matías Nicolás Echeveste Cardozo
namespace Turnos.Api.Dominio;

/// <summary>Validación de la cédula de identidad uruguaya (dígito verificador módulo 10).</summary>
public static class Cedula
{
    private static readonly int[] Pesos = [2, 9, 8, 7, 6, 3, 4];

    /// <summary>Quita puntos, guiones y espacios; completa con ceros a la izquierda hasta 8 dígitos.</summary>
    public static string? Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var digitos = new string(texto.Where(c => c is not ('.' or '-' or ' ')).ToArray());
        if (digitos.Length is < 7 or > 8 || !digitos.All(char.IsAsciiDigit)) return null;
        return digitos.PadLeft(8, '0');
    }

    public static bool EsValida(string? texto)
    {
        var cedula = Normalizar(texto);
        if (cedula is null) return false;
        var suma = 0;
        for (var i = 0; i < 7; i++) suma += (cedula[i] - '0') * Pesos[i];
        var verificador = (10 - suma % 10) % 10;
        return verificador == cedula[7] - '0';
    }
}
