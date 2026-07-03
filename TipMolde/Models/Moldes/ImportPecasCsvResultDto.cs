using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class ImportPecasCsvResultDto
{
    [JsonPropertyName("moldeId")]
    public int MoldeId { get; set; }

    [JsonPropertyName("referenciaMolde")]
    public string ReferenciaMolde { get; set; } = string.Empty;

    [JsonPropertyName("massaMolde")]
    public string MassaMolde { get; set; } = string.Empty;

    [JsonPropertyName("totalLinhasPecaLidas")]
    public int TotalLinhasPecaLidas { get; set; }

    [JsonPropertyName("totalPecasConsolidadas")]
    public int TotalPecasConsolidadas { get; set; }

    [JsonPropertyName("totalQuantidadeConsolidada")]
    public int TotalQuantidadeConsolidada { get; set; }

    [JsonPropertyName("pecasImportadas")]
    public List<PecaDto> PecasImportadas { get; set; } = [];
}
