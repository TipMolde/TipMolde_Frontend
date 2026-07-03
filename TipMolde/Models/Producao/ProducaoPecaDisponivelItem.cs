namespace TipMolde.Models;

public sealed class ProducaoPecaDisponivelItem
{
    public int MoldeId { get; init; }
    public int PecaId { get; init; }
    public int EncomendaMolde_id { get; init; }
    public int PrioridadeMolde { get; init; }
    public int PrioridadePeca { get; init; }
    public int Quantidade { get; init; }
    public string NumeroMolde { get; init; } = string.Empty;
    public string NomeMolde { get; init; } = string.Empty;
    public string NumeroEncomendaCliente { get; init; } = string.Empty;
    public string NomeCliente { get; init; } = string.Empty;
    public string Designacao { get; init; } = string.Empty;
    public string NumeroPeca { get; init; } = string.Empty;
    public DateTime DataEntregaPrevista { get; init; }
    public string UltimoEstadoGlobal { get; init; } = string.Empty;
    public string UltimaFaseGlobal { get; init; } = string.Empty;
    public int? ProximaFaseId { get; init; }
    public string ProximaFaseNome { get; init; } = string.Empty;
    public string FaseTrabalho { get; init; } = string.Empty;
    public string ProximoPasso { get; init; } = string.Empty;
    public string ResumoFases { get; init; } = string.Empty;
    public Dictionary<int, RegistoProducaoDto?> UltimosRegistosPorFase { get; init; } =
        new();

    public string NumeroMoldeDisplay => string.IsNullOrWhiteSpace(NumeroMolde) ? "Molde sem numero" : NumeroMolde;
    public string NomeMoldeDisplay => string.IsNullOrWhiteSpace(NomeMolde) ? "Molde sem nome" : NomeMolde;
    public string NumeroEncomendaDisplay => string.IsNullOrWhiteSpace(NumeroEncomendaCliente) ? "Sem numero" : NumeroEncomendaCliente;
    public string NomeClienteDisplay => string.IsNullOrWhiteSpace(NomeCliente) ? "Cliente nao definido" : NomeCliente;
    public string DesignacaoDisplay => string.IsNullOrWhiteSpace(Designacao) ? "Peca sem designacao" : Designacao;
    public string NumeroPecaDisplay => string.IsNullOrWhiteSpace(NumeroPeca) ? "Sem numero" : NumeroPeca;
    public string EntregaDisplay => DataEntregaPrevista > DateTime.MinValue
        ? DataEntregaPrevista.ToString("dd/MM/yyyy")
        : "Sem data";
    public string PrioridadeResumo => $"Molde P{PrioridadeMolde} | Peca P{PrioridadePeca}";
    public string EstadoAtualDisplay => string.IsNullOrWhiteSpace(UltimoEstadoGlobal)
        ? "Sem historico de producao"
        : $"{UltimoEstadoGlobal.Replace('_', ' ')} em {UltimaFaseGlobal}";
    public string ProximaFaseDisplay => string.IsNullOrWhiteSpace(ProximaFaseNome) ? "Sem fase definida" : ProximaFaseNome;
    public string FaseTrabalhoDisplay => string.IsNullOrWhiteSpace(FaseTrabalho) ? ProximaFaseDisplay : FaseTrabalho;
    public string ProximoPassoDisplay => string.IsNullOrWhiteSpace(ProximoPasso) ? "Sem proximo passo sugerido" : ProximoPasso;
}
