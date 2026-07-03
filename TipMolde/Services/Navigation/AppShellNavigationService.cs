using TipMolde.Diagnostics;
using TipMolde.View;

namespace TipMolde.Services;

public sealed class AppShellNavigationService : INavigationService
{
    private readonly AppShell _appShell;

    public AppShellNavigationService(AppShell appShell)
    {
        _appShell = appShell;
    }

    public Task GoToAsync(string route)
    {
        return GetShell().GoToAsync(route);
    }

    public Task GoToAsync(string route, IDictionary<string, object> parameters)
    {
        return GetShell().GoToAsync(route, parameters);
    }

    public async Task GoBackAsync()
    {
        try
        {
            var shell = GetShell();

            if (shell.Navigation.ModalStack.Count > 0)
            {
                await shell.Navigation.PopModalAsync();
                return;
            }

            if (shell.Navigation.NavigationStack.Count > 1)
                await shell.Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            TaskMonitor.ReportException("TipMolde.Services.Navigation.AppShellNavigationService.GoBackAsync", ex);
        }
    }

    public async Task NavigateToDashboardAfterLoginAsync()
    {
        _appShell.ShowDashboardRouteOnly();
        await GetShell().GoToAsync("//DashboardPage");
        await _appShell.RefreshNavigationAsync();
        _appShell.HideAuthenticationItem();
    }

    public async Task NavigateToAuthenticationAsync()
    {
        _appShell.ShowAuthenticationOnly();
        _appShell.FlyoutIsPresented = false;
        await GetShell().GoToAsync("//AutenticacaoPage");
    }

    public void OpenFlyout()
    {
        var shell = GetShell();

        if (shell.FlyoutBehavior == FlyoutBehavior.Disabled)
            return;

        shell.FlyoutIsPresented = true;
    }

    private Shell GetShell()
    {
        return Shell.Current ?? _appShell;
    }
}
