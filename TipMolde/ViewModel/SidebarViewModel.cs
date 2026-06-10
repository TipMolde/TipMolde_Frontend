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
    private bool canViewProducao;

    [ObservableProperty]
    private bool canViewMaquinas;

    [ObservableProperty]
    private bool canViewDesenho;

    public async Task EnsureLoadedAsync(bool forceRefresh = false)
    {
        if (_isLoaded && !forceRefresh)
            return;

        await _authorizationService.GetCurrentRoleAsync(forceRefresh);
        UpdateVisibility();
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
    private async Task OpenDefinicoesAsync()
    {
        await NavigateToFeatureAsync(AppFeature.Definicoes, "//Definicoes", "Definicoes");
    }

    private void UpdateVisibility()
    {
        CanViewDashboard = _authorizationService.CanAccess(AppFeature.Dashboard);
        CanViewUtilizadores = _authorizationService.CanAccess(AppFeature.Utilizadores);
        CanViewClientes = _authorizationService.CanAccess(AppFeature.Clientes);
        CanViewEncomendas = _authorizationService.CanAccess(AppFeature.Encomendas);
        CanViewProducao = _authorizationService.CanAccess(AppFeature.Producao);
        CanViewMaquinas = _authorizationService.CanAccess(AppFeature.Maquinas);
        CanViewDesenho = _authorizationService.CanAccess(AppFeature.Desenho);
    }

    private async Task NavigateToFeatureAsync(AppFeature feature, string route, string featureDisplayName)
    {
        try
        {
            await EnsureLoadedAsync();

            if (!_authorizationService.CanAccess(feature))
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
