using TipMolde.Diagnostics;

namespace TipMolde.Services;

public static class ShellNavigationService
{
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
