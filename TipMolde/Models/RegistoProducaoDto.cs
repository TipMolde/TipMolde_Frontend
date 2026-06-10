using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class RegistoProducaoDto
{
    [JsonPropertyName("registo_Producao_id")]
    public int RegistoProducaoId { get; set; }

    [JsonPropertyName("estado_producao")]
    public string EstadoProducao { get; set; } = string.Empty;

    [JsonPropertyName("data_hora")]
    public DateTime DataHora { get; set; }

    [JsonPropertyName("fase_id")]
    public int FaseId { get; set; }

    [JsonPropertyName("operador_id")]
    public int GestorProducaoId { get; set; }

    [JsonPropertyName("peca_id")]
    public int PecaId { get; set; }

    [JsonPropertyName("maquina_id")]
    public int? MaquinaId { get; set; }
}

public sealed class EstadoProducaoOption
{
    public string Value { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}
