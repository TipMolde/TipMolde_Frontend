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

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("numeroEncomendaCliente")]
    public string NumeroEncomendaCliente { get; set; } = string.Empty;

    [JsonPropertyName("numeroMolde")]
    public string NumeroMolde { get; set; } = string.Empty;

    public string NumeroEncomendaClienteDisplay => string.IsNullOrWhiteSpace(NumeroEncomendaCliente)
        ? "Encomenda sem numero"
        : NumeroEncomendaCliente;

    public string NumeroMoldeDisplay => string.IsNullOrWhiteSpace(NumeroMolde)
        ? "Molde sem numero"
        : NumeroMolde;

    public string DataEntregaPrevistaDisplay => DataEntregaPrevista == default
        ? "Data por definir"
        : DataEntregaPrevista.ToString("dd/MM/yyyy");

    public string QuantidadeDisplay => Quantidade <= 0
        ? "Quantidade por definir"
        : Quantidade.ToString();

    public string ContextoDisplay => $"{NumeroEncomendaClienteDisplay} | prioridade {Prioridade} | entrega {DataEntregaPrevistaDisplay}";
}
