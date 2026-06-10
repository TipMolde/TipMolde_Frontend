namespace TipMolde.Models;

public sealed class RegistoProducaoMaquinaOption
{
    public int? MaquinaId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public MaquinaItem? Maquina { get; init; }
    public bool IsNenhumaMaquina => !MaquinaId.HasValue;
}
