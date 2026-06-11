namespace TipMolde.Models;

public sealed class RelatorioExportRequest
{
    public string TipoRelatorio { get; set; } = string.Empty;
    public int MoldeId { get; set; }
    public string NumeroMolde { get; set; } = string.Empty;
    public int EncomendaMoldeId { get; set; }
    public int? FichaProducaoId { get; set; }
}
