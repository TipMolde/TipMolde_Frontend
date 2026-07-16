using CommunityToolkit.Mvvm.ComponentModel;
using TipMolde.Models;

namespace TipMolde.ViewModel;

/// <summary>
/// Representa uma peca selecionavel no fluxo de rececao de material.
/// </summary>
public partial class SelectablePecaRececaoItem : ObservableObject
{
    /// <summary>
    /// Construtor do item selecionavel de rececao.
    /// </summary>
    /// <param name="peca">DTO de peca usado para inicializar o item de UI.</param>
    public SelectablePecaRececaoItem(PecaDto peca)
    {
        PecaId = peca.PecaId;
        NumeroPeca = peca.NumeroPeca;
        Designacao = peca.Designacao;
        Prioridade = peca.Prioridade;
        Quantidade = peca.Quantidade;
        MaterialDesignacao = peca.MaterialDesignacao;
    }

    public int PecaId { get; }
    public string NumeroPeca { get; }
    public string Designacao { get; }
    public int Prioridade { get; }
    public int Quantidade { get; }
    public string MaterialDesignacao { get; }

    [ObservableProperty]
    private bool isSelected;

    public string NumeroPecaDisplay => string.IsNullOrWhiteSpace(NumeroPeca) ? "Sem número" : NumeroPeca;
    public string DesignacaoDisplay => string.IsNullOrWhiteSpace(Designacao) ? NumeroPecaDisplay : Designacao;
    public string MaterialDisplay => string.IsNullOrWhiteSpace(MaterialDesignacao) ? "Material não definido" : MaterialDesignacao;
    public string QuantidadeDisplay => $"Qtd: {Quantidade}";
    public string PrioridadeDisplay => $"Prioridade {Prioridade}";
}
