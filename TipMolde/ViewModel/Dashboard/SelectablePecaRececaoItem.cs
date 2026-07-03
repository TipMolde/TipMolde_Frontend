using CommunityToolkit.Mvvm.ComponentModel;
using TipMolde.Models;

namespace TipMolde.ViewModel;

public partial class SelectablePecaRececaoItem : ObservableObject
{
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

    public string NumeroPecaDisplay => string.IsNullOrWhiteSpace(NumeroPeca) ? "Sem numero" : NumeroPeca;
    public string DesignacaoDisplay => string.IsNullOrWhiteSpace(Designacao) ? NumeroPecaDisplay : Designacao;
    public string MaterialDisplay => string.IsNullOrWhiteSpace(MaterialDesignacao) ? "Material nao definido" : MaterialDesignacao;
    public string QuantidadeDisplay => $"Qtd: {Quantidade}";
    public string PrioridadeDisplay => $"Prioridade {Prioridade}";
}
