using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class IndustrialEventoDto
{
    [JsonPropertyName("eventoMaquinaIndustrial_id")]
    public int EventoMaquinaIndustrial_id { get; set; }

    [JsonPropertyName("sessaoMaquinaIndustrial_id")]
    public int? SessaoMaquinaIndustrial_id { get; set; }

    [JsonPropertyName("maquina_id")]
    public int Maquina_id { get; set; }

    [JsonPropertyName("ipMaquina")]
    public string IpMaquina { get; set; } = string.Empty;

    [JsonPropertyName("protocolo")]
    public string Protocolo { get; set; } = string.Empty;

    [JsonPropertyName("estadoMaquina")]
    public string EstadoMaquina { get; set; } = string.Empty;

    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; set; }

    [JsonPropertyName("programa")]
    public string? Programa { get; set; }

    [JsonPropertyName("contadorPecas")]
    public int? ContadorPecas { get; set; }

    [JsonPropertyName("camposEmFalta")]
    public string? CamposEmFalta { get; set; }

    [JsonPropertyName("estadoResolucao")]
    public string EstadoResolucao { get; set; } = string.Empty;

    public string EstadoMaquinaDisplay => string.IsNullOrWhiteSpace(EstadoMaquina)
        ? "Sem estado"
        : EstadoMaquina.Replace('_', ' ');

    public string OccurredAtDisplay => OccurredAt > DateTime.MinValue
        ? OccurredAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
        : "Sem data";
}
