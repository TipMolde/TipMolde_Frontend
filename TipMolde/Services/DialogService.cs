namespace TipMolde.Services;

public sealed class DialogService : IDialogService
{
    public Page GetCurrentPage()
    {
        return Shell.Current?.CurrentPage
            ?? throw new InvalidOperationException("Nao foi possivel obter a pagina atual.");
    }

    public async Task<string> ShowOptionsAsync(string message, string action)
    {
        return await GetCurrentPage().DisplayActionSheet(
            $"Opcoes para {message}",
            "Cancelar",
            null,
            action);
    }

    public async Task<bool> ConfirmDeleteAsync(string message)
    {
        return await GetCurrentPage().DisplayAlert(
            "Confirmar eliminacao",
            $"Tem a certeza que pretende eliminar {message}?",
            "Eliminar",
            "Cancelar");
    }

    public async Task ShowInfoAsync(string title, string message)
    {
        await GetCurrentPage().DisplayAlert(title, message, "OK");
    }

    public async Task ShowSuccessAsync(string title, string message)
    {
        await GetCurrentPage().DisplayAlert(title, message, "OK");
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        await GetCurrentPage().DisplayAlert(title, message, "OK");
    }
}
