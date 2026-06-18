using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class SidebarViewModel : ObservableObject
{
    private readonly AuthorizationService _authorizationService;
    private readonly IDialogService _dialogService;
    private bool _isLoaded;

    public SidebarViewModel(
        AuthorizationService authorizationService,
        IDialogService dialogService)
    {
        _authorizationService = authorizationService;
        _dialogService = dialogService;
    }

    [ObservableProperty]
    private bool canViewDashboard;

    [ObservableProperty]
    private bool canViewUtilizadores;

    [ObservableProperty]
    private bool canViewClientes;

    [ObservableProperty]
    private bool canViewEncomendas;

    [ObservableProperty]
    private bool canViewPedidosMaterial;

    [ObservableProperty]
    private bool canViewProducao;

    [ObservableProperty]
    private bool canViewMaquinas;

    [ObservableProperty]
    private bool canViewDesenho;

    [ObservableProperty]
    private bool canViewProjetos;

    [ObservableProperty]
    private bool canViewRelatorios;

    public async Task EnsureLoadedAsync(bool forceRefresh = false)
    {
        if (_isLoaded && !forceRefresh)
            return;

        await UpdateVisibilityAsync(forceRefresh);
        _isLoaded = true;
    }

    [RelayCommand]
    private async Task OpenDashboardAsync()
    {
        await NavigateToFeatureAsync(AppFeature.Dashboard, "//MainPage", "Dashboard");
    }

    [RelayCommand]
    private async Task OpenUtilizadoresAsync()
    {
        await NavigateToFeatureAsync(AppFeature.Utilizadores, "//Utilizadores", "Gestao de utilizadores");
    }

    [RelayCommand]
    private async Task OpenClientesAsync()
    {
        await NavigateToFeatureAsync(AppFeature.Clientes, "//Clientes", "Clientes");
    }

    [RelayCommand]
    private async Task OpenEncomendasAsync()
    {
        await NavigateToFeatureAsync(AppFeature.Encomendas, "//Encomendas", "Encomendas");
    }

    [RelayCommand]
    private async Task OpenPedidosMaterialAsync()
    {
        await NavigateToFeatureAsync(AppFeature.PedidosMaterial, "//PedidosMaterial", "Pedidos de material");
    }

    [RelayCommand]
    private async Task OpenProducaoAsync()
    {
        await NavigateToFeatureAsync(AppFeature.Producao, "//Producao", "Producao");
    }

    [RelayCommand]
    private async Task OpenMaquinasAsync()
    {
        await NavigateToFeatureAsync(AppFeature.Maquinas, "//Maquinas", "Maquinas");
    }

    [RelayCommand]
    private async Task OpenDesenhoAsync()
    {
        await NavigateToFeatureAsync(AppFeature.Desenho, "//Desenho", "Desenho");
    }

    [RelayCommand]
    private async Task OpenProjetosAsync()
    {
        await NavigateToFeatureAsync(AppFeature.Desenho, "//Projetos", "Projetos");
    }

    [RelayCommand]
    private async Task OpenRelatoriosAsync()
    {
        await NavigateToFeatureAsync(AppFeature.Relatorios, "//Relatorios", "Relatorios");
    }

    [RelayCommand]
    private async Task OpenDefinicoesAsync()
    {
        await NavigateToFeatureAsync(AppFeature.Definicoes, "//Definicoes", "Definicoes");
    }

    private async Task UpdateVisibilityAsync(bool forceRefresh = false)
    {
        CanViewDashboard = await _authorizationService.CanAccessAsync(AppFeature.Dashboard, forceRefresh);
        CanViewUtilizadores = await _authorizationService.CanAccessAsync(AppFeature.Utilizadores);
        CanViewClientes = await _authorizationService.CanAccessAsync(AppFeature.Clientes);
        CanViewEncomendas = await _authorizationService.CanAccessAsync(AppFeature.Encomendas);
        CanViewPedidosMaterial = await _authorizationService.CanAccessAsync(AppFeature.PedidosMaterial);
        CanViewProducao = await _authorizationService.CanAccessAsync(AppFeature.Producao);
        CanViewMaquinas = await _authorizationService.CanAccessAsync(AppFeature.Maquinas);
        CanViewDesenho = await _authorizationService.CanAccessAsync(AppFeature.Desenho);
        CanViewProjetos = await _authorizationService.CanAccessAsync(AppFeature.Desenho);
        CanViewRelatorios = await _authorizationService.CanAccessAsync(AppFeature.Relatorios);
    }

    private async Task NavigateToFeatureAsync(AppFeature feature, string route, string featureDisplayName)
    {
        try
        {
            await EnsureLoadedAsync();

            if (!await _authorizationService.CanAccessAsync(feature))
            {
                await _dialogService.ShowErrorAsync(
                    "Acesso restrito",
                    $"Nao tens permissao para aceder a {featureDisplayName}.");
                return;
            }

            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Erro", ex.Message);
        }
    }
}
