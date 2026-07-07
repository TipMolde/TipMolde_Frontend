using TipMolde.Diagnostics;
using TipMolde.View;

namespace TipMolde.Services;

/// <summary>
/// Implementa navegacao da app sobre o <see cref="AppShell"/>.
/// </summary>
/// <remarks>
/// Encapsula regras de retorno, transicao entre autenticacao e area autenticada
/// e operacoes de flyout para manter a UI desacoplada de <see cref="Shell.Current"/>.
/// </remarks>
public sealed class AppShellNavigationService : INavigationService
{
    private readonly AppShell _appShell;

    /// <summary>
    /// Construtor do servico de navegacao baseado em shell.
    /// </summary>
    /// <param name="appShell">Shell principal da aplicacao.</param>
    public AppShellNavigationService(AppShell appShell)
    {
        _appShell = appShell;
    }

    /// <summary>
    /// Navega para uma rota sem parametros.
    /// </summary>
    /// <param name="route">Rota Shell de destino.</param>
    /// <returns>Tarefa assincrona da navegacao.</returns>
    public Task GoToAsync(string route)
    {
        return GetShell().GoToAsync(route);
    }

    /// <summary>
    /// Navega para uma rota transportando parametros para a pagina de destino.
    /// </summary>
    /// <param name="route">Rota Shell de destino.</param>
    /// <param name="parameters">Parametros a disponibilizar no destino.</param>
    /// <returns>Tarefa assincrona da navegacao.</returns>
    public Task GoToAsync(string route, IDictionary<string, object> parameters)
    {
        return GetShell().GoToAsync(route, parameters);
    }

    /// <summary>
    /// Regressa a pagina anterior dando prioridade a modais abertos.
    /// </summary>
    /// <returns>Tarefa assincrona da navegacao de retorno.</returns>
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

    /// <summary>
    /// Navega para o dashboard apos login e recompõe o menu autenticado.
    /// </summary>
    /// <returns>Tarefa assincrona da transicao para a area autenticada.</returns>
    public async Task NavigateToDashboardAfterLoginAsync()
    {
        _appShell.ShowDashboardRouteOnly();
        await GetShell().GoToAsync("//DashboardPage");
        await _appShell.RefreshNavigationAsync();
        _appShell.HideAuthenticationItem();
    }

    /// <summary>
    /// Navega para o ecran de autenticacao e limita temporariamente o menu visivel.
    /// </summary>
    /// <returns>Tarefa assincrona da navegacao para autenticacao.</returns>
    public async Task NavigateToAuthenticationAsync()
    {
        _appShell.ShowAuthenticationOnly();
        _appShell.FlyoutIsPresented = false;
        await GetShell().GoToAsync("//AutenticacaoPage");
    }

    /// <summary>
    /// Abre o flyout quando o layout atual suporta menu de navegacao.
    /// </summary>
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
