using System.Text.Json.Serialization;
using TipMolde.Helper;

namespace TipMolde.Models;

public sealed class FilaGlobalMoldeItemDto
{
    [JsonPropertyName("encomendaMolde_id")]
    public int EncomendaMoldeId { get; set; }

    [JsonPropertyName("encomenda_id")]
    public int EncomendaId { get; set; }

    [JsonPropertyName("molde_id")]
    public int MoldeId { get; set; }

    [JsonPropertyName("prioridade")]
    public int Prioridade { get; set; }

    [JsonPropertyName("dataEntregaPrevista")]
    public DateTime DataEntregaPrevista { get; set; }

    [JsonPropertyName("quantidade")]
    public int Quantidade { get; set; }

    [JsonPropertyName("numeroEncomendaCliente")]
    public string NumeroEncomendaCliente { get; set; } = string.Empty;

    [JsonPropertyName("nomeCliente")]
    public string NomeCliente { get; set; } = string.Empty;

    [JsonPropertyName("numeroMolde")]
    public string NumeroMolde { get; set; } = string.Empty;

    [JsonPropertyName("nomeMolde")]
    public string NomeMolde { get; set; } = string.Empty;

    [JsonPropertyName("imagemCapaPath")]
    public string ImagemCapaPath { get; set; } = string.Empty;

    [JsonPropertyName("estadoEncomenda")]
    public string EstadoEncomenda { get; set; } = string.Empty;

    public string NumeroMoldeDisplay => string.IsNullOrWhiteSpace(NumeroMolde) ? "Molde sem numero" : NumeroMolde;
    public string NomeMoldeDisplay => string.IsNullOrWhiteSpace(NomeMolde) ? "Molde sem nome" : NomeMolde;
    public string NumeroEncomendaDisplay => string.IsNullOrWhiteSpace(NumeroEncomendaCliente) ? "Encomenda sem numero" : NumeroEncomendaCliente;
    public string NomeClienteDisplay => string.IsNullOrWhiteSpace(NomeCliente) ? "Cliente nao definido" : NomeCliente;
    public string EstadoEncomendaDisplay => string.IsNullOrWhiteSpace(EstadoEncomenda) ? "Sem estado" : EstadoEncomenda.Replace('_', ' ');
    public string DataEntregaPrevistaDisplay => DataEntregaPrevista == default
        ? "Data nao definida"
        : DataEntregaPrevista.ToString("dd/MM/yyyy");
    public string ImagemCapaSource => MoldeImageSourceHelper.Resolve(ImagemCapaPath);
    public bool IsEntregaPassada => DataEntregaPrevista.Date < DateTime.Today;
    public bool IsEntregaConcluida =>
        string.Equals(EstadoEncomenda?.Trim(), "CONCLUIDA", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(EstadoEncomenda?.Trim(), "CONCLUIDO", StringComparison.OrdinalIgnoreCase);

    public string EstadoPlanificacaoDisplay
    {
        get
        {
            if (IsEntregaConcluida)
                return "Entregue";

            return IsEntregaPassada ? "Entrega ultrapassada" : "Previsto";
        }
    }
}
