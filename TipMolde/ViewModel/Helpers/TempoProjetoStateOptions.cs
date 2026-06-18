using TipMolde.Models;

namespace TipMolde.ViewModel.Helpers;

public static class TempoProjetoStateOptions
{
    private static readonly string[] PrimeiroEstado = ["INICIADO"];
    private static readonly string[] DepoisDeIniciado = ["PAUSADO", "CONCLUIDO"];
    private static readonly string[] DepoisDePausado = ["RETOMADO"];
    private static readonly string[] DepoisDeRetomado = ["PAUSADO", "CONCLUIDO"];
    private static readonly string[] DepoisDeConcluido = ["INICIADO"];

    public static IReadOnlyList<string> GetAllowedStates(IEnumerable<RegistoTempoProjetoDto> historico)
    {
        var ultimoEstado = historico
            .OrderByDescending(item => item.Data_hora)
            .Select(item => NormalizeState(item.Estado_tempo))
            .FirstOrDefault();

        return ultimoEstado switch
        {
            null => PrimeiroEstado,
            "INICIADO" => DepoisDeIniciado,
            "PAUSADO" => DepoisDePausado,
            "RETOMADO" => DepoisDeRetomado,
            "CONCLUIDO" => DepoisDeConcluido,
            _ => PrimeiroEstado
        };
    }

    private static string NormalizeState(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
    }
}
