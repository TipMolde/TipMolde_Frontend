using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;

namespace TipMolde.ViewModel;

public partial class EncomendaDetalheViewModel : ObservableObject
{
    private const string ValorNaoDefinido = "Nao definido";

    private readonly EncomendasService _encomendasService;
    private readonly MoldesService _moldesService;
    private readonly ClientesService _clientesService;

    public EncomendaDetalheViewModel(
        EncomendasService encomendasService,
        MoldesService moldesService,
        ClientesService clientesService)
    {
        _encomendasService = encomendasService;
        _moldesService = moldesService;
        _clientesService = clientesService;
    }

    public ObservableCollection<EncomendaMoldeItemDto> Moldes { get; } = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private int encomenda_id;

    [ObservableProperty]
    private string numeroEncomendaCliente = string.Empty;

    [ObservableProperty]
    private string nomeCliente = string.Empty;

    [ObservableProperty]
    private string nomeServicoCliente = string.Empty;

    [ObservableProperty]
    private string nomeResponsavelCliente = string.Empty;

    [ObservableProperty]
    private string numeroProjetoCliente = string.Empty;

    [ObservableProperty]
    private string estado = string.Empty;

    [ObservableProperty]
    private DateTime dataRegisto;

    [ObservableProperty]
    private int quantidadeTotalPrevista;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasMoldes => Moldes.Count > 0;
    public bool HasNoMoldes => Moldes.Count == 0;
    public string NumeroEncomendaClienteDisplay => string.IsNullOrWhiteSpace(NumeroEncomendaCliente) ? ValorNaoDefinido : NumeroEncomendaCliente;
    public string NomeClienteDisplay => string.IsNullOrWhiteSpace(NomeCliente) ? ValorNaoDefinido : NomeCliente;
    public string NomeServicoClienteDisplay => string.IsNullOrWhiteSpace(NomeServicoCliente) ? ValorNaoDefinido : NomeServicoCliente;
    public string NomeResponsavelClienteDisplay => string.IsNullOrWhiteSpace(NomeResponsavelCliente) ? ValorNaoDefinido : NomeResponsavelCliente;
    public string NumeroProjetoClienteDisplay => string.IsNullOrWhiteSpace(NumeroProjetoCliente) ? ValorNaoDefinido : NumeroProjetoCliente;
    public string EstadoDisplay => string.IsNullOrWhiteSpace(Estado) ? ValorNaoDefinido : Estado.Replace('_', ' ');
    public int TotalMoldesAssociados => Moldes.Count;
    public string EmptyMoldesMessage => "Esta encomenda ainda nao tem moldes associados.";

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnNomeClienteChanged(string value) => OnPropertyChanged(nameof(NomeClienteDisplay));
    partial void OnNumeroEncomendaClienteChanged(string value) => OnPropertyChanged(nameof(NumeroEncomendaClienteDisplay));
    partial void OnNomeServicoClienteChanged(string value) => OnPropertyChanged(nameof(NomeServicoClienteDisplay));
    partial void OnNomeResponsavelClienteChanged(string value) => OnPropertyChanged(nameof(NomeResponsavelClienteDisplay));
    partial void OnNumeroProjetoClienteChanged(string value) => OnPropertyChanged(nameof(NumeroProjetoClienteDisplay));
    partial void OnEstadoChanged(string value) => OnPropertyChanged(nameof(EstadoDisplay));
    partial void OnQuantidadeTotalPrevistaChanged(int value) => OnPropertyChanged(nameof(QuantidadeTotalPrevista));

    public async Task LoadAsync(int encomendaId)
    {
        Encomenda_id = encomendaId;
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            var encomenda = await _encomendasService.GetByIdAsync(encomendaId);
            if (encomenda is null)
            {
                ErrorMessage = "Nao foi possivel carregar o detalhe da encomenda.";
                Moldes.Clear();
                NotifyMoldeStateChanged();
                return;
            }

            NumeroEncomendaCliente = encomenda.NumeroEncomendaCliente ?? string.Empty;
            NomeCliente = encomenda.NomeCliente ?? string.Empty;
            NomeServicoCliente = encomenda.NomeServicoCliente ?? string.Empty;
            NomeResponsavelCliente = encomenda.NomeResponsavelCliente ?? string.Empty;
            NumeroProjetoCliente = encomenda.NumeroProjetoCliente ?? string.Empty;
            Estado = encomenda.Estado ?? string.Empty;
            DataRegisto = encomenda.DataRegisto;

            if (string.IsNullOrWhiteSpace(NomeCliente) && encomenda.Cliente_id > 0)
            {
                var cliente = await _clientesService.GetByIdAsync(encomenda.Cliente_id);
                NomeCliente = cliente?.Nome?.Trim() ?? string.Empty;
            }

            var associacoesTask = _encomendasService.GetEncomendaMoldesByEncomendaIdAsync(encomendaId, 1, 100);
            var moldesTask = _moldesService.GetByEncomendaIdAsync(encomendaId, 1, 100);

            await Task.WhenAll(associacoesTask, moldesTask);

            var associacoes = associacoesTask.Result?.Items ?? [];
            var moldes = moldesTask.Result?.Items ?? [];
            var moldesPorId = moldes.ToDictionary(molde => molde.MoldeId);

            Moldes.Clear();

            foreach (var associacao in associacoes.OrderBy(item => item.Prioridade).ThenBy(item => item.Molde_id))
            {
                moldesPorId.TryGetValue(associacao.Molde_id, out var molde);

                Moldes.Add(new EncomendaMoldeItemDto
                {
                    MoldeId = associacao.Molde_id,
                    NumeroMolde = FirstNonEmpty(associacao.NumeroMolde, molde?.Numero),
                    NomeMolde = molde?.Nome ?? string.Empty,
                    DescricaoMolde = molde?.Descricao ?? string.Empty,
                    NumeroCavidades = molde?.Numero_cavidades ?? 0,
                    Quantidade = associacao.Quantidade,
                    Prioridade = associacao.Prioridade,
                    DataEntregaPrevista = associacao.DataEntregaPrevista
                });
            }

            QuantidadeTotalPrevista = associacoes.Sum(item => item.Quantidade);
            NotifyMoldeStateChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Moldes.Clear();
            NotifyMoldeStateChanged();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AbrirMoldeAsync(EncomendaMoldeItemDto? molde)
    {
        if (molde is null || molde.MoldeId <= 0)
            return;

        await Shell.Current.GoToAsync($"{nameof(MoldeDetalhePage)}?molde_id={molde.MoldeId}");
    }

    [RelayCommand]
    private async Task VoltarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private void NotifyMoldeStateChanged()
    {
        OnPropertyChanged(nameof(HasMoldes));
        OnPropertyChanged(nameof(HasNoMoldes));
        OnPropertyChanged(nameof(TotalMoldesAssociados));
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }
}
