using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;

namespace TipMolde.ViewModel;

public partial class DashboardViewModel : ObservableObject
{
    private const string ValorNaoDefinido = "Nao definido";

    private readonly EncomendasService _encomendasService;
    private readonly MoldesService _moldesService;

    public DashboardViewModel(EncomendasService encomendasService, MoldesService moldesService)
    {
        _encomendasService = encomendasService;
        _moldesService = moldesService;
    }

    [ObservableProperty]
    private bool isLoadingHero;

    [ObservableProperty]
    private string heroErrorMessage = string.Empty;

    [ObservableProperty]
    private MoldeDto? moldeMaisProximo;

    [ObservableProperty]
    private EncomendaResumoDto? encomendaMaisProxima;

    [ObservableProperty]
    private EncomendaMoldeDto? entregaMoldeMaisProxima;

    [ObservableProperty]
    private MoldeCicloVidaDashboardDto? dashboardMaisProximo;

    public bool HasHeroError => !string.IsNullOrWhiteSpace(HeroErrorMessage);
    public bool HasMoldeEntregaDashboard =>
        MoldeMaisProximo is not null &&
        EncomendaMaisProxima is not null &&
        EntregaMoldeMaisProxima is not null &&
        DashboardMaisProximo is not null;
    public bool HasNoMoldeEntregaDashboard => !IsLoadingHero && !HasHeroError && !HasMoldeEntregaDashboard;
    public string NomeMoldeMaisProximoDisplay => FirstNonEmpty(MoldeMaisProximo?.Nome, MoldeMaisProximo?.Numero, EntregaMoldeMaisProxima?.NumeroMolde);
    public string NumeroMoldeMaisProximoDisplay => FirstNonEmpty(MoldeMaisProximo?.Numero, EntregaMoldeMaisProxima?.NumeroMolde);
    public string ClienteMoldeMaisProximoDisplay => FirstNonEmpty(EncomendaMaisProxima?.NomeClienteDisplay);
    public string DataEntregaMoldeMaisProximoDisplay => EntregaMoldeMaisProxima is null
        ? ValorNaoDefinido
        : EntregaMoldeMaisProxima.DataEntregaPrevista.ToString("dd/MM/yyyy");
    public string PercentagemConclusaoMaisProximoDisplay => DashboardMaisProximo is null
        ? ValorNaoDefinido
        : $"{DashboardMaisProximo.PercentagemConclusao:0.##}%";

    partial void OnIsLoadingHeroChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
    }

    partial void OnHeroErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasHeroError));
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
    }

    partial void OnMoldeMaisProximoChanged(MoldeDto? value)
    {
        OnPropertyChanged(nameof(HasMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
        OnPropertyChanged(nameof(NomeMoldeMaisProximoDisplay));
        OnPropertyChanged(nameof(NumeroMoldeMaisProximoDisplay));
        AbrirDashboardMoldeCommand.NotifyCanExecuteChanged();
    }

    partial void OnEncomendaMaisProximaChanged(EncomendaResumoDto? value)
    {
        OnPropertyChanged(nameof(HasMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
        OnPropertyChanged(nameof(ClienteMoldeMaisProximoDisplay));
    }

    partial void OnEntregaMoldeMaisProximaChanged(EncomendaMoldeDto? value)
    {
        OnPropertyChanged(nameof(HasMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
        OnPropertyChanged(nameof(NumeroMoldeMaisProximoDisplay));
        OnPropertyChanged(nameof(DataEntregaMoldeMaisProximoDisplay));
        AbrirDashboardMoldeCommand.NotifyCanExecuteChanged();
    }

    partial void OnDashboardMaisProximoChanged(MoldeCicloVidaDashboardDto? value)
    {
        OnPropertyChanged(nameof(HasMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
        OnPropertyChanged(nameof(PercentagemConclusaoMaisProximoDisplay));
    }

    public async Task LoadAsync()
    {
        if (IsLoadingHero)
            return;

        IsLoadingHero = true;
        HeroErrorMessage = string.Empty;

        try
        {
            var encomendas = await GetAllEncomendasAsync();

            if (encomendas.Count == 0)
            {
                HeroErrorMessage = "Nao existem encomendas em producao para apresentar no dashboard.";
                LimparDashboard();
                return;
            }

            var melhorCandidato = await EncontrarMoldeMaisProximoAsync(encomendas);

            if (melhorCandidato is null)
            {
                HeroErrorMessage = "Nao foi encontrada uma data de entrega valida para os moldes em producao.";
                LimparDashboard();
                return;
            }

            var moldeTask = _moldesService.GetByIdAsync(melhorCandidato.EncomendaMolde.Molde_id);
            var dashboardTask = _moldesService.GetDashboardCicloVidaAsync(melhorCandidato.EncomendaMolde.Molde_id);

            await Task.WhenAll(moldeTask, dashboardTask);

            var molde = moldeTask.Result;
            var dashboard = dashboardTask.Result;

            if (molde is null || dashboard is null)
            {
                HeroErrorMessage = "Nao foi possivel carregar o resumo do molde mais proximo de entrega.";
                LimparDashboard();
                return;
            }

            MoldeMaisProximo = molde;
            EncomendaMaisProxima = melhorCandidato.Encomenda;
            EntregaMoldeMaisProxima = melhorCandidato.EncomendaMolde;
            DashboardMaisProximo = dashboard;
        }
        catch (Exception ex)
        {
            HeroErrorMessage = ex.Message;
            LimparDashboard();
        }
        finally
        {
            IsLoadingHero = false;
        }
    }

    [RelayCommand(CanExecute = nameof(HasMoldeEntregaDashboard))]
    private async Task AbrirDashboardMoldeAsync()
    {
        if (EntregaMoldeMaisProxima is null || EntregaMoldeMaisProxima.Molde_id <= 0)
            return;

        await Shell.Current.GoToAsync($"{nameof(MoldeDetalhePage)}?molde_id={EntregaMoldeMaisProxima.Molde_id}");
    }

    private async Task<List<EncomendaResumoDto>> GetAllEncomendasAsync()
    {
        var primeiraPagina = await _encomendasService.GetEncomendasNaoConcluidasAsync(1, 100);
        if (primeiraPagina is null)
            return [];

        var encomendas = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _encomendasService.GetEncomendasNaoConcluidasAsync(page, 100);
            if (pagina?.Items is null)
                continue;

            encomendas.AddRange(pagina.Items);
        }

        return encomendas;
    }

    private async Task<MoldeEntregaCandidato?> EncontrarMoldeMaisProximoAsync(IReadOnlyCollection<EncomendaResumoDto> encomendas)
    {
        var candidatos = new List<MoldeEntregaCandidato>();

        foreach (var encomenda in encomendas)
        {
            var moldes = await GetAllEncomendaMoldesAsync(encomenda.Encomenda_id);

            foreach (var encomendaMolde in moldes.Where(item => item.DataEntregaPrevista > DateTime.MinValue))
                candidatos.Add(new MoldeEntregaCandidato(encomenda, encomendaMolde));
        }

        return candidatos
            .OrderBy(item => item.EncomendaMolde.DataEntregaPrevista)
            .ThenBy(item => item.EncomendaMolde.Prioridade)
            .FirstOrDefault();
    }

    private async Task<List<EncomendaMoldeDto>> GetAllEncomendaMoldesAsync(int encomendaId)
    {
        var primeiraPagina = await _encomendasService.GetEncomendaMoldesByEncomendaIdAsync(encomendaId, 1, 100);
        if (primeiraPagina is null)
            return [];

        var encomendaMoldes = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _encomendasService.GetEncomendaMoldesByEncomendaIdAsync(encomendaId, page, 100);
            if (pagina?.Items is null)
                continue;

            encomendaMoldes.AddRange(pagina.Items);
        }

        return encomendaMoldes;
    }

    private void LimparDashboard()
    {
        MoldeMaisProximo = null;
        EncomendaMaisProxima = null;
        EntregaMoldeMaisProxima = null;
        DashboardMaisProximo = null;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return ValorNaoDefinido;
    }

    private sealed record MoldeEntregaCandidato(EncomendaResumoDto Encomenda, EncomendaMoldeDto EncomendaMolde);
}
