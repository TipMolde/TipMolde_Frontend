using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class MoldeCicloVidaDashboardDto
{
    [JsonPropertyName("moldeId")]
    public int MoldeId { get; set; }

    [JsonPropertyName("numeroMolde")]
    public string NumeroMolde { get; set; } = string.Empty;

    [JsonPropertyName("totalPecas")]
    public int TotalPecas { get; set; }

    [JsonPropertyName("maquinacao")]
    public int Maquinacao { get; set; }

    [JsonPropertyName("erosao")]
    public int Erosao { get; set; }

    [JsonPropertyName("montagem")]
    public int Montagem { get; set; }

    [JsonPropertyName("emTrabalho")]
    public int EmTrabalho { get; set; }

    [JsonPropertyName("concluidas")]
    public int Concluidas { get; set; }

    [JsonPropertyName("materialPendente")]
    public int MaterialPendente { get; set; }

    [JsonPropertyName("percentagemConclusao")]
    public decimal PercentagemConclusao { get; set; }
}
