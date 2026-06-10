using CommunityToolkit.Mvvm.ComponentModel;
using TipMolde.Helper;

namespace TipMolde.Models;

public partial class EncomendaMoldeItemDto : ObservableObject
{
    public int EncomendaMoldeId { get; set; }
    public int MoldeId { get; set; }
    public string NumeroMolde { get; set; } = string.Empty;
    public string NomeMolde { get; set; } = string.Empty;
    public string DescricaoMolde { get; set; } = string.Empty;
    public string ImagemCapaPath { get; set; } = string.Empty;
    public int NumeroCavidades { get; set; }

    [ObservableProperty]
    private int quantidade;

    [ObservableProperty]
    private int prioridade;

    [ObservableProperty]
    private DateTime dataEntregaPrevista;

    [ObservableProperty]
    private int quantidadePorEntregar;

    [ObservableProperty]
    private bool isEntregue;

    [ObservableProperty]
    private bool canEditarEntrega = true;

    public string NumeroMoldeDisplay => string.IsNullOrWhiteSpace(NumeroMolde) ? "Molde sem numero" : NumeroMolde;
    public string NomeMoldeDisplay => string.IsNullOrWhiteSpace(NomeMolde) ? "Molde sem nome" : NomeMolde;
    public string DescricaoMoldeDisplay => string.IsNullOrWhiteSpace(DescricaoMolde) ? "Sem descricao disponivel." : DescricaoMolde;
    public string ImagemCapaSource => MoldeImageSourceHelper.Resolve(ImagemCapaPath);
    public string QuantidadePorEntregarDisplay => $"{Math.Max(0, QuantidadePorEntregar)}";
    public string EstadoEntregaDisplay => IsEntregue ? "Entregue" : "Por entregar";
    public bool CanRegistarEntrega => QuantidadePorEntregar > 0 && CanEditarEntrega;
    public bool HasEntregasRegistadas => QuantidadePorEntregar < Quantidade;
    public bool CanEditarDataEntrega => CanEditarEntrega && !HasEntregasRegistadas;

    partial void OnQuantidadePorEntregarChanged(int value)
    {
        if (value <= 0 && !IsEntregue)
            IsEntregue = true;
        else if (value > 0 && IsEntregue)
            IsEntregue = false;

        OnPropertyChanged(nameof(QuantidadePorEntregarDisplay));
        OnPropertyChanged(nameof(EstadoEntregaDisplay));
        OnPropertyChanged(nameof(CanRegistarEntrega));
        OnPropertyChanged(nameof(HasEntregasRegistadas));
        OnPropertyChanged(nameof(CanEditarDataEntrega));
    }

    partial void OnIsEntregueChanged(bool value)
    {
        OnPropertyChanged(nameof(EstadoEntregaDisplay));
        OnPropertyChanged(nameof(CanRegistarEntrega));
    }

    partial void OnCanEditarEntregaChanged(bool value)
    {
        OnPropertyChanged(nameof(CanRegistarEntrega));
        OnPropertyChanged(nameof(CanEditarDataEntrega));
    }
}
