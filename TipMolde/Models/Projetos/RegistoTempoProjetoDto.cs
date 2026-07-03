using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class RegistoTempoProjetoDto
{
    [JsonPropertyName("registo_Tempo_Projeto_id")]
    public int Registo_Tempo_Projeto_id { get; set; }

    [JsonPropertyName("estado_tempo")]
    public string Estado_tempo { get; set; } = string.Empty;

    [JsonPropertyName("data_hora")]
    public DateTime Data_hora { get; set; }

    [JsonPropertyName("projeto_id")]
    public int Projeto_id { get; set; }

    [JsonPropertyName("autor_id")]
    public int Autor_id { get; set; }

    public string EstadoDisplay => string.IsNullOrWhiteSpace(Estado_tempo) ? "Sem estado" : Estado_tempo.Replace('_', ' ');
    public string DataHoraDisplay => Data_hora == default ? "Sem data" : Data_hora.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}
