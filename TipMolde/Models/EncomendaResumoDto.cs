using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class EncomendaResumoDto
{
    [JsonPropertyName("encomenda_id")]
    public int Encomenda_id { get; set; }

    [JsonPropertyName("numeroEncomendaCliente")]
    public string NumeroEncomendaCliente { get; set; } = string.Empty;

    [JsonPropertyName("numeroProjetoCliente")]
    public string NumeroProjetoCliente { get; set; } = string.Empty;

    [JsonPropertyName("nomeServicoCliente")]
    public string NomeServicoCliente { get; set; } = string.Empty;

    [JsonPropertyName("nomeResponsavelCliente")]
    public string NomeResponsavelCliente { get; set; } = string.Empty;

    [JsonPropertyName("dataRegisto")]
    public DateTime DataRegisto { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("cliente_id")]
    public int Cliente_id { get; set; }

    [JsonPropertyName("nomeCliente")]
    public string NomeCliente { get; set; } = string.Empty;
}