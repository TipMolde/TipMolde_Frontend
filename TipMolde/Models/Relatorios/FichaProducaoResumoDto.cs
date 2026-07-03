using System.Text.Json;
using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class FichaProducaoResumoDto
{
    [JsonPropertyName("fichaProducao_id")]
    public int FichaProducaoId { get; set; }

    [JsonPropertyName("tipo")]
    public JsonElement Tipo { get; set; }

    [JsonPropertyName("dataCriacao")]
    public DateTime DataCriacao { get; set; }

    [JsonPropertyName("encomendaMolde_id")]
    public int EncomendaMoldeId { get; set; }

    public string TipoDisplay => Tipo.ValueKind switch
    {
        JsonValueKind.String => NormalizeTipo(Tipo.GetString()),
        JsonValueKind.Number when Tipo.TryGetInt32(out var tipoNumerico) => tipoNumerico switch
        {
            0 => "FLT",
            1 => "FRE",
            2 => "FRM",
            3 => "FRA",
            4 => "FOP",
            _ => $"Tipo {tipoNumerico}"
        },
        _ => "Desconhecido"
    };

    private static string NormalizeTipo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Desconhecido";

        return value.Trim().ToUpperInvariant() switch
        {
            "FTL" => "FLT",
            var normalized => normalized
        };
    }
}
