using TipMolde.Diagnostics;

namespace TipMolde.Services;

/// <summary>
/// Disponibiliza uma navegacao de retorno simples baseada em <see cref="Shell.Current"/>.
/// </summary>
/// <remarks>
/// Este helper legado continua util em pontos onde ainda nao foi injetado
/// um <see cref="INavigationService"/> completo.
/// </remarks>
public static class ShellNavigationService
{
    /// <summary>
    /// Regressa a pagina anterior respeitando stacks modal e normal.
    /// </summary>
    /// <returns>Tarefa assincrona da navegacao de retorno.</returns>
    public static async Task GoBackAsync()
    {
        try
        {
            var shell = Shell.Current;
            if (shell is null)
                return;

            if (shell.Navigation.ModalStack.Count > 0)
            {
                await shell.Navigation.PopModalAsync();
                return;
            }

            if (shell.Navigation.NavigationStack.Count > 1)
            {
                await shell.Navigation.PopAsync();
            }
        }
        catch (Exception ex)
        {
            TaskMonitor.ReportException("TipMolde.Services.Navigation.ShellNavigationService.GoBackAsync", ex);
        }
    }
}
