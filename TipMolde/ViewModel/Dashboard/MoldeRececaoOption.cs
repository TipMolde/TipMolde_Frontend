namespace TipMolde.ViewModel;

public sealed class MoldeRececaoOption
{
    public int MoldeId { get; init; }
    public string NumeroMolde { get; init; } = string.Empty;
    public string NumeroEncomendaCliente { get; init; } = string.Empty;
    public DateTime DataEntregaPrevista { get; init; }
    public int Prioridade { get; init; }

    public string NumeroMoldeDisplay => string.IsNullOrWhiteSpace(NumeroMolde) ? "Molde sem numero" : NumeroMolde;
    public string EncomendaDisplay => string.IsNullOrWhiteSpace(NumeroEncomendaCliente) ? "Sem numero" : NumeroEncomendaCliente;
    public string DataEntregaDisplay => DataEntregaPrevista > DateTime.MinValue
        ? DataEntregaPrevista.ToString("dd/MM/yyyy")
        : "Nao definida";
    public string DisplayName => $"{NumeroMoldeDisplay} | {DataEntregaDisplay}";
}
