using TipMolde.Services;

namespace TipMolde.View;

public partial class App : Application
{
    public App(
        AppShell appShell,
        SessaoPersistidaService sessaoPersistidaService,
        ThemePreferenceService themePreferenceService)
    {
        InitializeComponent();
        themePreferenceService.ApplyStoredTheme();

        MainPage = appShell;
        _ = InitializeAsync(appShell, sessaoPersistidaService);
    }

    private static async Task InitializeAsync(AppShell appShell, SessaoPersistidaService sessaoPersistidaService)
    {
        var hasRestoredSession = await sessaoPersistidaService.TryRestoreSessionAsync();
        var targetRoute = hasRestoredSession ? "//MainPage" : "//AutenticacaoPage";

        await appShell.RefreshNavigationAsync();
        await MainThread.InvokeOnMainThreadAsync(() => appShell.GoToAsync(targetRoute));
    }
}
