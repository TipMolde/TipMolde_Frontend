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

    [JsonIgnore]
    public string EstadoDisplay => string.IsNullOrWhiteSpace(Estado)
        ? "Sem estado"
        : Estado.Replace('_', ' ');

    [JsonIgnore]
    public string NumeroEncomendaClienteDisplay => string.IsNullOrWhiteSpace(NumeroEncomendaCliente)
        ? "Sem numero de encomenda"
        : NumeroEncomendaCliente;

    [JsonIgnore]
    public string NomeClienteDisplay => string.IsNullOrWhiteSpace(NomeCliente)
        ? "Cliente nao identificado"
        : NomeCliente;

    [JsonIgnore]
    public string NomeServicoClienteDisplay => string.IsNullOrWhiteSpace(NomeServicoCliente)
        ? "Servico nao definido"
        : NomeServicoCliente;

    [JsonIgnore]
    public string NomeResponsavelClienteDisplay => string.IsNullOrWhiteSpace(NomeResponsavelCliente)
        ? "Responsavel nao definido"
        : NomeResponsavelCliente;

    [JsonIgnore]
    public string NumeroProjetoClienteDisplay => string.IsNullOrWhiteSpace(NumeroProjetoCliente)
        ? "Projeto nao definido"
        : NumeroProjetoCliente;
}
