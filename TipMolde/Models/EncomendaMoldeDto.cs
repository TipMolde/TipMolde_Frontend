using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class EncomendaMoldeDto
{
    [JsonPropertyName("encomendaMolde_id")]
    public int EncomendaMolde_id { get; set; }

    [JsonPropertyName("encomenda_id")]
    public int Encomenda_id { get; set; }

    [JsonPropertyName("molde_id")]
    public int Molde_id { get; set; }

    [JsonPropertyName("quantidade")]
    public int Quantidade { get; set; }

    [JsonPropertyName("prioridade")]
    public int Prioridade { get; set; }

    [JsonPropertyName("dataEntregaPrevista")]
    public DateTime DataEntregaPrevista { get; set; }

    [JsonPropertyName("numeroEncomendaCliente")]
    public string NumeroEncomendaCliente { get; set; } = string.Empty;

    [JsonPropertyName("numeroMolde")]
    public string NumeroMolde { get; set; } = string.Empty;
}
