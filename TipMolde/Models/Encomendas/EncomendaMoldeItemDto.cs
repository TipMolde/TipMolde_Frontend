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
    private string estado = string.Empty;

    [ObservableProperty]
    private bool canGerirMolde = true;

    public string NumeroMoldeDisplay => string.IsNullOrWhiteSpace(NumeroMolde) ? "Molde sem numero" : NumeroMolde;
    public string NomeMoldeDisplay => string.IsNullOrWhiteSpace(NomeMolde) ? "Molde sem nome" : NomeMolde;
    public string DescricaoMoldeDisplay => string.IsNullOrWhiteSpace(DescricaoMolde) ? "Sem descricao disponivel." : DescricaoMolde;
    public string ImagemCapaSource => MoldeImageSourceHelper.Resolve(ImagemCapaPath);
    public string EstadoDisplay => string.IsNullOrWhiteSpace(Estado) ? "Sem estado" : Estado.Replace('_', ' ');
    public bool IsPendente => IsEstado("PENDENTE");
    public bool IsEmProducao => IsEstado("EM_PRODUCAO");
    public bool IsConcluido => IsEstado("CONCLUIDO");
    public bool CanIniciarProducao => CanGerirMolde && IsPendente;
    public bool CanConcluirMolde => CanGerirMolde && IsEmProducao;
    public bool CanEditarDataEntrega => CanGerirMolde && !IsConcluido;

    partial void OnEstadoChanged(string value)
    {
        OnPropertyChanged(nameof(EstadoDisplay));
        OnPropertyChanged(nameof(IsPendente));
        OnPropertyChanged(nameof(IsEmProducao));
        OnPropertyChanged(nameof(IsConcluido));
        OnPropertyChanged(nameof(CanIniciarProducao));
        OnPropertyChanged(nameof(CanConcluirMolde));
        OnPropertyChanged(nameof(CanEditarDataEntrega));
    }

    partial void OnCanGerirMoldeChanged(bool value)
    {
        OnPropertyChanged(nameof(CanIniciarProducao));
        OnPropertyChanged(nameof(CanConcluirMolde));
        OnPropertyChanged(nameof(CanEditarDataEntrega));
    }

    private bool IsEstado(string estado) => string.Equals(Estado, estado, StringComparison.OrdinalIgnoreCase);
}
