namespace TipMolde.Models;

public sealed class EncomendaMoldeItemDto
{
    public int EncomendaMoldeId { get; set; }
    public int MoldeId { get; set; }
    public string NumeroMolde { get; set; } = string.Empty;
    public string NomeMolde { get; set; } = string.Empty;
    public string DescricaoMolde { get; set; } = string.Empty;
    public int NumeroCavidades { get; set; }
    public int Quantidade { get; set; }
    public int Prioridade { get; set; }
    public DateTime DataEntregaPrevista { get; set; }

    public string NumeroMoldeDisplay => string.IsNullOrWhiteSpace(NumeroMolde) ? "Molde sem numero" : NumeroMolde;
    public string NomeMoldeDisplay => string.IsNullOrWhiteSpace(NomeMolde) ? "Molde sem nome" : NomeMolde;
    public string DescricaoMoldeDisplay => string.IsNullOrWhiteSpace(DescricaoMolde) ? "Sem descricao disponivel." : DescricaoMolde;
}
