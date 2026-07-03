using TipMolde.Diagnostics;
using TipMolde.Services;

namespace TipMolde.View;

public partial class AppShell : Shell
{
    private static readonly IReadOnlyDictionary<string, AppFeature> RootRoutePermissions =
        new Dictionary<string, AppFeature>(StringComparer.OrdinalIgnoreCase)
        {
            ["DashboardPage"] = AppFeature.Dashboard,
            ["Utilizadores"] = AppFeature.Utilizadores,
            ["Clientes"] = AppFeature.Clientes,
            ["Encomendas"] = AppFeature.Encomendas,
            ["PedidosMaterial"] = AppFeature.PedidosMaterial,
            ["Producao"] = AppFeature.Producao,
            ["Maquinas"] = AppFeature.Maquinas,
            ["Desenho"] = AppFeature.Desenho,
            ["Projetos"] = AppFeature.Desenho,
            ["FopGeralPage"] = AppFeature.Relatorios,
            ["Relatorios"] = AppFeature.Relatorios,
            ["Definicoes"] = AppFeature.Definicoes
        };

    private readonly AuthorizationService _authorizationService;
    private readonly ResponsiveLayoutService _responsiveLayoutService;
    private bool _isNavigationRefreshRunning;

    public AppShell(
        AuthorizationService authorizationService,
        ResponsiveLayoutService responsiveLayoutService)
    {
        _authorizationService = authorizationService;
        _responsiveLayoutService = responsiveLayoutService;

        InitializeComponent();
        ConfigureAdaptiveNavigation();
        _responsiveLayoutService.PropertyChanged += OnResponsiveLayoutChanged;

        Loaded += OnLoaded;
        Navigated += OnNavigated;
        Navigating += OnNavigating;

        Routing.RegisterRoute(nameof(AdicionarUtilizadorPage), typeof(AdicionarUtilizadorPage));
        Routing.RegisterRoute(nameof(AdicionarClientePage), typeof(AdicionarClientePage));
        Routing.RegisterRoute(nameof(AdicionarEncomendaPage), typeof(AdicionarEncomendaPage));
        Routing.RegisterRoute(nameof(AdicionarMoldePage), typeof(AdicionarMoldePage));
        Routing.RegisterRoute(nameof(EditarMoldePage), typeof(EditarMoldePage));
        Routing.RegisterRoute(nameof(AdicionarPecaPage), typeof(AdicionarPecaPage));
        Routing.RegisterRoute(nameof(EditarPecaPage), typeof(EditarPecaPage));
        Routing.RegisterRoute(nameof(EditarMaquinaPage), typeof(EditarMaquinaPage));
        Routing.RegisterRoute(nameof(MaquinaDetalhePage), typeof(MaquinaDetalhePage));
        Routing.RegisterRoute(nameof(EditarClientePage), typeof(EditarClientePage));
        Routing.RegisterRoute(nameof(ClienteDetalhePage), typeof(ClienteDetalhePage));
        Routing.RegisterRoute(nameof(EncomendaDetalhePage), typeof(EncomendaDetalhePage));
        Routing.RegisterRoute(nameof(FilaTrabalhoPage), typeof(FilaTrabalhoPage));
        Routing.RegisterRoute(nameof(MoldeDetalhePage), typeof(MoldeDetalhePage));
        Routing.RegisterRoute(nameof(MaquinasPage), typeof(MaquinasPage));
        Routing.RegisterRoute(nameof(DesenhoPage), typeof(DesenhoPage));
        Routing.RegisterRoute(nameof(ProjetosPage), typeof(ProjetosPage));
        Routing.RegisterRoute(nameof(AdicionarProjetoPage), typeof(AdicionarProjetoPage));
        Routing.RegisterRoute(nameof(ProjetoDetalhePage), typeof(ProjetoDetalhePage));
        Routing.RegisterRoute(nameof(FopGeralPage), typeof(FopGeralPage));
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
            PedidosMaterialShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.PedidosMaterial);
            ProducaoShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.Producao);
            MaquinasShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.Maquinas);
            DesenhoShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.Desenho);
            ProjetosShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.Desenho);
            RelatoriosShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.Relatorios);
            DefinicoesShellItem.FlyoutItemIsVisible = await _authorizationService.CanAccessAsync(AppFeature.Definicoes);
        }
        finally
        {
            _isNavigationRefreshRunning = false;
        }
    }

    public void ShowAuthenticationOnly()
    {
        AutenticacaoShellItem.FlyoutItemIsVisible = true;
        DashboardShellItem.FlyoutItemIsVisible = false;
        UtilizadoresShellItem.FlyoutItemIsVisible = false;
        ClientesShellItem.FlyoutItemIsVisible = false;
        EncomendasShellItem.FlyoutItemIsVisible = false;
        PedidosMaterialShellItem.FlyoutItemIsVisible = false;
        ProducaoShellItem.FlyoutItemIsVisible = false;
        MaquinasShellItem.FlyoutItemIsVisible = false;
        DesenhoShellItem.FlyoutItemIsVisible = false;
        ProjetosShellItem.FlyoutItemIsVisible = false;
        RelatoriosShellItem.FlyoutItemIsVisible = false;
        DefinicoesShellItem.FlyoutItemIsVisible = false;
    }

    public void ShowAuthenticatedLandingOnly()
    {
        ShowDashboardRouteOnly();
    }

    public void ShowDashboardRouteOnly()
    {
        AutenticacaoShellItem.FlyoutItemIsVisible = true;
        DashboardShellItem.FlyoutItemIsVisible = true;
        UtilizadoresShellItem.FlyoutItemIsVisible = false;
        ClientesShellItem.FlyoutItemIsVisible = false;
        EncomendasShellItem.FlyoutItemIsVisible = false;
        PedidosMaterialShellItem.FlyoutItemIsVisible = false;
        ProducaoShellItem.FlyoutItemIsVisible = false;
        MaquinasShellItem.FlyoutItemIsVisible = false;
        DesenhoShellItem.FlyoutItemIsVisible = false;
        ProjetosShellItem.FlyoutItemIsVisible = false;
        RelatoriosShellItem.FlyoutItemIsVisible = false;
        DefinicoesShellItem.FlyoutItemIsVisible = false;
    }

    public void HideAuthenticationItem()
    {
        AutenticacaoShellItem.FlyoutItemIsVisible = false;
    }

    private void ConfigureAdaptiveNavigation()
    {
        FlyoutBehavior = _responsiveLayoutService.ShowNavigationMenu
            ? FlyoutBehavior.Flyout
            : FlyoutBehavior.Disabled;

        if (FlyoutBehavior == FlyoutBehavior.Disabled)
            FlyoutIsPresented = false;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        TaskMonitor.Observe("TipMolde.View.AppShell.OnLoaded", OnLoadedAsync());
    }

    private async Task OnLoadedAsync()
    {
        if (IsAuthenticationRoute(CurrentState?.Location?.OriginalString))
            return;

        await RefreshNavigationAsync();
    }

    private void OnNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        TaskMonitor.Observe("TipMolde.View.AppShell.OnNavigated", OnNavigatedAsync(e));
    }

    private async Task OnNavigatedAsync(ShellNavigatedEventArgs e)
    {
        if (IsAuthenticationRoute(e.Current?.Location?.OriginalString))
            return;

        await RefreshNavigationAsync();
    }

    private void OnNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        TaskMonitor.Observe("TipMolde.View.AppShell.OnNavigating", OnNavigatingAsync(e));
    }

    private async Task OnNavigatingAsync(ShellNavigatingEventArgs e)
    {
        var feature = ResolveFeatureFromRoute(e.Target.Location.OriginalString);
        if (feature is null)
            return;

        bool isAuthorized;
        try
        {
            isAuthorized = await _authorizationService.CanAccessAsync(feature.Value);
        }
        catch (Exception ex)
        {
            TaskMonitor.ReportException("TipMolde.View.AppShell.OnNavigatingAsync.Authorization", ex);
            e.Cancel();
            await ShowNavigationErrorAsync(
                "Erro de autorizacao",
                "Nao foi possivel validar o acesso a esta area. Tenta novamente.");
            return;
        }

        if (isAuthorized)
            return;

        e.Cancel();

        await ShowNavigationErrorAsync(
            "Acesso restrito",
            "Nao tens permissao para aceder a esta area.");
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

    private static bool IsAuthenticationRoute(string? route) =>
        !string.IsNullOrWhiteSpace(route) &&
        route.Contains("AutenticacaoPage", StringComparison.OrdinalIgnoreCase);

    private async Task ShowNavigationErrorAsync(string title, string message)
    {
        if (CurrentPage is null)
            return;

        await CurrentPage.DisplayAlert(title, message, "Fechar");
    }

    private void OnResponsiveLayoutChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.PropertyName) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.LayoutMode), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.ShowNavigationMenu), StringComparison.Ordinal))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(ConfigureAdaptiveNavigation);
    }
}
