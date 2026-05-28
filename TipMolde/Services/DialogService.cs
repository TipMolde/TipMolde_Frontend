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

    public async Task<string?> ShowSelectionAsync(string title, string cancel, params string[] options)
    {
        var selection = await GetCurrentPage().DisplayActionSheet(
            title,
            cancel,
            null,
            options);

        return selection == cancel ? null : selection;
    }

    public async Task<string?> PromptAsync(
        string title,
        string message,
        string accept = "OK",
        string cancel = "Cancelar",
        string initialValue = "",
        int maxLength = -1,
        Keyboard? keyboard = null,
        string placeholder = "")
    {
        return await GetCurrentPage().DisplayPromptAsync(
            title,
            message,
            accept,
            cancel,
            placeholder,
            maxLength,
            keyboard,
            initialValue);
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
