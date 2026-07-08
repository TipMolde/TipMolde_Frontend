using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class ProjetoComRevisoesDto : ProjetoBaseDto
{
    [JsonPropertyName("revisoes")]
    public List<RevisaoDto> Revisoes { get; set; } = [];

    public string RevisoesResumoDisplay => Revisoes.Count == 0 ? "Sem revisoes associadas" : $"{Revisoes.Count} revisoes";
}
