using TipMolde.Diagnostics;
using TipMolde.Services;

namespace TipMolde.View;

/// <summary>
/// Representa a aplicacao MAUI principal do frontend.
/// </summary>
/// <remarks>
/// Orquestra o arranque da shell, a restauracao de sessao e a ativacao
/// do layout responsivo global antes do utilizador interagir com a UI.
/// </remarks>
public partial class App : Application
{
    private readonly AppShell _appShell;
    private readonly ResponsiveLayoutService _responsiveLayoutService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private bool _startupInitialized;

    /// <summary>
    /// Construtor da aplicacao principal.
    /// </summary>
    /// <param name="appShell">Shell principal usada como raiz visual da app.</param>
    /// <param name="sessaoPersistidaService">Servico que tenta restaurar a sessao autenticada.</param>
    /// <param name="responsiveLayoutService">Servico global de layout responsivo.</param>
    public App(
        AppShell appShell,
        SessaoPersistidaService sessaoPersistidaService,
        ResponsiveLayoutService responsiveLayoutService)
    {
        StartupCrashLogger.RegisterGlobalHandlers();

        try
        {
            InitializeComponent();
            ThemePreferenceService.ApplyStoredTheme();
            _appShell = appShell;
            _responsiveLayoutService = responsiveLayoutService;
            _sessaoPersistidaService = sessaoPersistidaService;

            MainPage = appShell;
            _appShell.Loaded += OnAppShellLoaded;
        }
        catch (Exception ex)
        {
            StartupCrashLogger.LogException("TipMolde.View.App constructor", ex);
            throw;
        }
    }

    /// <summary>
    /// Cria a janela principal e liga o rastreio de redimensionamento responsivo.
    /// </summary>
    /// <param name="activationState">Estado opcional de ativacao fornecido pela plataforma.</param>
    /// <returns>Janela principal da aplicacao.</returns>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        UpdateResponsiveLayout(window);
        window.SizeChanged += OnWindowSizeChanged;
        return window;
    }

    private void OnAppShellLoaded(object? sender, EventArgs e)
    {
        if (_startupInitialized)
            return;

        _startupInitialized = true;
        _appShell.Loaded -= OnAppShellLoaded;

        TaskMonitor.Observe("TipMolde.View.App.OnAppShellLoaded", InitializeAsync());
    }

    private async Task InitializeAsync()
    {
        try
        {
            var hasRestoredSession = await _sessaoPersistidaService.TryRestoreSessionAsync();
            var targetRoute = hasRestoredSession ? "//DashboardPage" : "//AutenticacaoPage";

            if (hasRestoredSession)
                _appShell.ShowDashboardRouteOnly();
            else
                _appShell.ShowAuthenticationOnly();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await _appShell.GoToAsync(targetRoute);

                if (hasRestoredSession)
                {
                    await _appShell.RefreshNavigationAsync();
                    _appShell.HideAuthenticationItem();
                }
            });
        }
        catch (Exception ex)
        {
            StartupCrashLogger.LogException("TipMolde.View.App.InitializeAsync", ex);
            await RecoverFromInitializationFailureAsync();
        }
    }

    private async Task RecoverFromInitializationFailureAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                _appShell.ShowAuthenticationOnly();
                await _appShell.GoToAsync("//AutenticacaoPage");
            }
            catch (Exception navigationEx)
            {
                StartupCrashLogger.LogException("TipMolde.View.App.RecoverFromInitializationFailureAsync.Navigation", navigationEx);
            }

            if (_appShell.CurrentPage is not null)
            {
                await _appShell.CurrentPage.DisplayAlert(
                    "Erro no arranque",
                    "Nao foi possivel concluir a inicializacao da aplicacao. Tenta iniciar sessao novamente.",
                    "Fechar");
            }
        });
    }

    private void OnWindowSizeChanged(object? sender, EventArgs e)
    {
        if (sender is Window window)
            UpdateResponsiveLayout(window);
    }

    private void UpdateResponsiveLayout(Window window)
    {
        var width = window.Width;
        if (double.IsNaN(width) || width <= 0)
            width = DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density;

        _responsiveLayoutService.UpdateWidth(width);
    }
}
