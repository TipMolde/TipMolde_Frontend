using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class ProjetoDto
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

    [JsonPropertyName("numeroMolde")]
    public string NumeroMolde { get; set; } = string.Empty;

    public string NomeProjetoDisplay => string.IsNullOrWhiteSpace(NomeProjeto) ? "Projeto sem nome" : NomeProjeto;
    public string TipoProjetoDisplay => string.IsNullOrWhiteSpace(TipoProjeto) ? "Sem tipo" : TipoProjeto.Replace('_', ' ');
    public string SoftwareUtilizadoDisplay => string.IsNullOrWhiteSpace(SoftwareUtilizado) ? "Software nao definido" : SoftwareUtilizado;
    public string CaminhoPastaServidorDisplay => string.IsNullOrWhiteSpace(CaminhoPastaServidor) ? "Caminho nao definido" : CaminhoPastaServidor;
    public string MoldeDisplay => string.IsNullOrWhiteSpace(NumeroMolde)
        ? "Molde nao definido"
        : NumeroMolde;
    public string ResumoDisplay => $"{NomeProjetoDisplay} | {TipoProjetoDisplay}";
}
