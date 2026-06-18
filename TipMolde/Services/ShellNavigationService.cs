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
        catch
        {
            // Se a navegacao anterior nao existir, evitamos quebrar a aplicacao.
        }
    }
}
