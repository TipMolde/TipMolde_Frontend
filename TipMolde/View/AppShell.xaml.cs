using TipMolde.Services;

namespace TipMolde.View;

public partial class AppShell : Shell
{
    private static readonly IReadOnlyDictionary<string, AppFeature> RootRoutePermissions =
        new Dictionary<string, AppFeature>(StringComparer.OrdinalIgnoreCase)
        {
            ["MainPage"] = AppFeature.Dashboard,
            ["Utilizadores"] = AppFeature.Utilizadores,
            ["Clientes"] = AppFeature.Clientes,
            ["Encomendas"] = AppFeature.Encomendas,
            ["Producao"] = AppFeature.Producao,
            ["Maquinas"] = AppFeature.Maquinas,
            ["Desenho"] = AppFeature.Desenho,
            ["Relatorios"] = AppFeature.Relatorios,
            ["Definicoes"] = AppFeature.Definicoes
        };

    private readonly AuthorizationService _authorizationService;
    private bool _isNavigationRefreshRunning;

    public AppShell(AuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;

        InitializeComponent();
        ConfigureAdaptiveNavigation();

        Loaded += OnLoaded;
        Navigated += OnNavigated;
        Navigating += OnNavigating;

        Routing.RegisterRoute(nameof(AdicionarUtilizadorPage), typeof(AdicionarUtilizadorPage));
        Routing.RegisterRoute(nameof(AdicionarClientePage), typeof(AdicionarClientePage));
        Routing.RegisterRoute(nameof(AdicionarEncomendaPage), typeof(AdicionarEncomendaPage));
        Routing.RegisterRoute(nameof(AdicionarMoldePage), typeof(AdicionarMoldePage));
        Routing.RegisterRoute(nameof(AdicionarPecaPage), typeof(AdicionarPecaPage));
        Routing.RegisterRoute(nameof(EditarPecaPage), typeof(EditarPecaPage));
        Routing.RegisterRoute(nameof(EditarMaquinaPage), typeof(EditarMaquinaPage));
        Routing.RegisterRoute(nameof(EditarClientePage), typeof(EditarClientePage));
        Routing.RegisterRoute(nameof(ClienteDetalhePage), typeof(ClienteDetalhePage));
        Routing.RegisterRoute(nameof(EncomendaDetalhePage), typeof(EncomendaDetalhePage));
        Routing.RegisterRoute(nameof(FilaTrabalhoPage), typeof(FilaTrabalhoPage));
        Routing.RegisterRoute(nameof(MoldeDetalhePage), typeof(MoldeDetalhePage));
        Routing.RegisterRoute(nameof(MaquinasPage), typeof(MaquinasPage));
        Routing.RegisterRoute(nameof(DesenhoPage), typeof(DesenhoPage));
        Routing.RegisterRoute(nameof(RelatoriosPage), typeof(RelatoriosPage));
        Routing.RegisterRoute(nameof(RegistoProducaoPage), typeof(RegistoProducaoPage));
    }

    public async Task RefreshNavigationAsync()
    {
        if (_isNavigationRefreshRunning)
            return;

        _isNavigationRefreshRunning = true;

        try
        {
            DashboardShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.Dashboard);
            UtilizadoresShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.Utilizadores);
            ClientesShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.Clientes);
            EncomendasShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.Encomendas);
            ProducaoShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.Producao);
            MaquinasShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.Maquinas);
            DesenhoShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.Desenho);
            RelatoriosShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.Relatorios);
            DefinicoesShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.Definicoes);
        }
        finally
        {
            _isNavigationRefreshRunning = false;
        }
    }

    private void ConfigureAdaptiveNavigation()
    {
        FlyoutBehavior = DeviceInfo.Current.Idiom == DeviceIdiom.Phone
            ? FlyoutBehavior.Flyout
            : FlyoutBehavior.Disabled;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        await RefreshNavigationAsync();
    }

    private async void OnNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        await RefreshNavigationAsync();
    }

    private async void OnNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        var feature = ResolveFeatureFromRoute(e.Target.Location.OriginalString);
        if (feature is null)
            return;

        var isAuthorized = await _authorizationService.CanAccessAsync(feature.Value);
        if (isAuthorized)
            return;

        e.Cancel();

        if (CurrentPage is not null)
        {
            await CurrentPage.DisplayAlert(
                "Acesso restrito",
                "Nao tens permissao para aceder a esta area.",
                "Fechar");
        }
    }

    private static AppFeature? ResolveFeatureFromRoute(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
            return null;

        foreach (var routePermission in RootRoutePermissions)
        {
            if (route.Contains(routePermission.Key, StringComparison.OrdinalIgnoreCase))
                return routePermission.Value;
        }

        return null;
    }
}
