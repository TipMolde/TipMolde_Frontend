namespace TipMolde.Models;

public sealed class RegistoProducaoHistoricoItem
{
    public int RegistoProducaoId { get; init; }
    public int FaseId { get; init; }
    public string FaseDisplay { get; init; } = string.Empty;
    public string EstadoProducao { get; init; } = string.Empty;
    public DateTime DataHora { get; init; }
    public int GestorProducaoId { get; init; }
    public int? MaquinaId { get; init; }
    public string MaquinaNome { get; init; } = string.Empty;

    public string EstadoDisplay => string.IsNullOrWhiteSpace(EstadoProducao)
        ? "Sem estado"
        : EstadoProducao.Replace('_', ' ');

    public string DataHoraDisplay => DataHora == default
        ? "Sem data"
        : DataHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public string MaquinaDisplay
    {
        get
        {
            if (!MaquinaId.HasValue)
                return "Sem maquina";

            return string.IsNullOrWhiteSpace(MaquinaNome) ? "Maquina atribuida" : MaquinaNome;
        }
    }

    public bool IsActive => IsEstado("PREPARACAO") || IsEstado("EM_CURSO");
    public bool IsClosed => IsEstado("CONCLUIDO");

    private bool IsEstado(string expected)
    {
        return string.Equals(
            EstadoProducao?.Trim(),
            expected,
            StringComparison.OrdinalIgnoreCase);
    }
}
