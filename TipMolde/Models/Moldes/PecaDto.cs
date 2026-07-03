using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class PecaDto
{
    [JsonPropertyName("pecaId")]
    public int PecaId { get; set; }

    [JsonPropertyName("numeroPeca")]
    public string NumeroPeca { get; set; } = string.Empty;

    [JsonPropertyName("designacao")]
    public string Designacao { get; set; } = string.Empty;

    [JsonPropertyName("prioridade")]
    public int Prioridade { get; set; }

    [JsonPropertyName("quantidade")]
    public int Quantidade { get; set; }

    [JsonPropertyName("referencia")]
    public string Referencia { get; set; } = string.Empty;

    [JsonPropertyName("materialDesignacao")]
    public string MaterialDesignacao { get; set; } = string.Empty;

    [JsonPropertyName("tratamentoTermico")]
    public string TratamentoTermico { get; set; } = string.Empty;

    [JsonPropertyName("massa")]
    public string Massa { get; set; } = string.Empty;

    [JsonPropertyName("observacao")]
    public string Observacao { get; set; } = string.Empty;

    [JsonPropertyName("materialRecebido")]
    public bool MaterialRecebido { get; set; }

    [JsonPropertyName("proximaFase_id")]
    public int? ProximaFase_id { get; set; }

    [JsonPropertyName("proximaFaseNome")]
    public string ProximaFaseNome { get; set; } = string.Empty;

    [JsonPropertyName("molde_id")]
    public int Molde_id { get; set; }
}
