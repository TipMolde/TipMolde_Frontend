using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class FaseProducaoItem
{
    [JsonPropertyName("fasesProducao_id")]
    public int FasesProducao_id { get; set; }

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("descricao")]
    public string Descricao { get; set; } = string.Empty;

    public string NomeDisplay => string.IsNullOrWhiteSpace(Nome) ? "Fase sem nome" : Nome.Replace('_', ' ');
    public string DescricaoDisplay => string.IsNullOrWhiteSpace(Descricao) ? "Sem descricao" : Descricao;
}
