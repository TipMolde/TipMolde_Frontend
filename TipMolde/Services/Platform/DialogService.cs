namespace TipMolde.Services;

public sealed class DialogService : IDialogService
{
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

    public async Task<string?> PromptAsync(string title, string message, PromptDialogOptions? options = null)
    {
        options ??= new PromptDialogOptions();

        return await GetCurrentPage().DisplayPromptAsync(
            title,
            message,
            options.Accept,
            options.Cancel,
            options.Placeholder,
            options.MaxLength,
            options.Keyboard,
            options.InitialValue);
    }

    public async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        return await GetCurrentPage().DisplayAlert(title, message, accept, cancel);
    }

    public async Task<bool> ConfirmDeleteAsync(string message)
    {
        return await ConfirmAsync(
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

    private static Page GetCurrentPage()
    {
        return Shell.Current?.CurrentPage
            ?? throw new InvalidOperationException("Nao foi possivel obter a pagina atual.");
    }
}
