using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class ProjetoComRevisoesDto
{
    [JsonPropertyName("projeto_id")]
    public int Projeto_id { get; set; }

    [JsonPropertyName("nomeProjeto")]
    public string NomeProjeto { get; set; } = string.Empty;

    [JsonPropertyName("softwareUtilizado")]
    public string SoftwareUtilizado { get; set; } = string.Empty;

    [JsonPropertyName("tipoProjeto")]
    public string TipoProjeto { get; set; } = string.Empty;

    [JsonPropertyName("caminhoPastaServidor")]
    public string CaminhoPastaServidor { get; set; } = string.Empty;

    [JsonPropertyName("molde_id")]
    public int Molde_id { get; set; }

    [JsonPropertyName("revisoes")]
    public List<RevisaoDto> Revisoes { get; set; } = [];

    public string NomeProjetoDisplay => string.IsNullOrWhiteSpace(NomeProjeto) ? $"Projeto {Projeto_id}" : NomeProjeto;
    public string TipoProjetoDisplay => string.IsNullOrWhiteSpace(TipoProjeto) ? "Sem tipo" : TipoProjeto.Replace('_', ' ');
    public string SoftwareUtilizadoDisplay => string.IsNullOrWhiteSpace(SoftwareUtilizado) ? "Software nao definido" : SoftwareUtilizado;
    public string CaminhoPastaServidorDisplay => string.IsNullOrWhiteSpace(CaminhoPastaServidor) ? "Caminho nao definido" : CaminhoPastaServidor;
    public string MoldeDisplay => Molde_id > 0 ? $"Molde #{Molde_id}" : "Molde nao definido";
    public string RevisoesResumoDisplay => Revisoes.Count == 0 ? "Sem revisoes associadas" : $"{Revisoes.Count} revisoes";
}
