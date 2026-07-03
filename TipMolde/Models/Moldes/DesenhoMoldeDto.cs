using TipMolde.Helper;

namespace TipMolde.Models;

public sealed class DesenhoMoldeItem
{
    public int EncomendaId { get; set; }
    public int MoldeId { get; set; }
    public int TotalPecas { get; set; }
    public string NumeroEncomendaCliente { get; set; } = string.Empty;
    public string NomeCliente { get; set; } = string.Empty;
    public string NomeServicoCliente { get; set; } = string.Empty;
    public string NumeroMolde { get; set; } = string.Empty;
    public string NomeMolde { get; set; } = string.Empty;
    public string DescricaoMolde { get; set; } = string.Empty;
    public string ImagemCapaPath { get; set; } = string.Empty;
    public string PecasResumoDisplay { get; set; } = string.Empty;
    public DateTime DataRegistoEncomenda { get; set; }
    public DateTime? DataEntregaPrevista { get; set; }

    public string NumeroEncomendaDisplay => string.IsNullOrWhiteSpace(NumeroEncomendaCliente) ? "Encomenda sem numero" : NumeroEncomendaCliente;
    public string NomeClienteDisplay => string.IsNullOrWhiteSpace(NomeCliente) ? "Cliente nao definido" : NomeCliente;
    public string NomeServicoDisplay => string.IsNullOrWhiteSpace(NomeServicoCliente) ? "Servico nao definido" : NomeServicoCliente;
    public string NumeroMoldeDisplay => string.IsNullOrWhiteSpace(NumeroMolde) ? "Molde sem numero" : NumeroMolde;
    public string NomeMoldeDisplay => string.IsNullOrWhiteSpace(NomeMolde) ? "Molde sem nome" : NomeMolde;
    public string DescricaoMoldeDisplay => string.IsNullOrWhiteSpace(DescricaoMolde) ? "Sem descricao disponivel." : DescricaoMolde;
    public string TotalPecasDisplay => $"{Math.Max(0, TotalPecas)}";
    public string PecasResumoDisplayText => string.IsNullOrWhiteSpace(PecasResumoDisplay)
        ? "Sem detalhes adicionais das pecas."
        : PecasResumoDisplay;
    public string DataEntregaPrevistaDisplay => DataEntregaPrevista.HasValue
        ? DataEntregaPrevista.Value.ToString("dd/MM/yyyy")
        : "Data por definir";
    public string ImagemCapaSource => MoldeImageSourceHelper.Resolve(ImagemCapaPath);
}
