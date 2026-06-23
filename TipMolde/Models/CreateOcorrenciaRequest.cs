using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class CreateOcorrenciaRequest
{
    [JsonPropertyName("EncomendaMolde_id")]
    public int EncomendaMoldeId { get; set; }

    [JsonPropertyName("Peca_id")]
    public int PecaId { get; set; }

    [JsonPropertyName("Responsavel_id")]
    public int ResponsavelId { get; set; }

    [JsonPropertyName("Ocorrencia")]
    public string Ocorrencia { get; set; } = string.Empty;

    [JsonPropertyName("Correcao")]
    public string? Correcao { get; set; }
}
