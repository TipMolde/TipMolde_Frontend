namespace TipMolde.Services;

/// <summary>
/// Encapsula dialogs modais apresentados pela pagina atual da shell.
/// </summary>
public sealed class DialogService : IDialogService
{
    /// <summary>
    /// Apresenta uma folha de acoes simples para uma mensagem contextual.
    /// </summary>
    /// <param name="message">Descricao resumida do contexto da acao.</param>
    /// <param name="action">Acao principal apresentada ao utilizador.</param>
    /// <returns>Opcao selecionada pelo utilizador.</returns>
    public async Task<string> ShowOptionsAsync(string message, string action)
    {
        return await GetCurrentPage().DisplayActionSheet(
            $"Opcoes para {message}",
            "Cancelar",
            null,
            action);
    }

    /// <summary>
    /// Apresenta uma lista de opcoes mutuamente exclusivas.
    /// </summary>
    /// <param name="title">Titulo do dialog.</param>
    /// <param name="cancel">Texto da opcao de cancelamento.</param>
    /// <param name="options">Opcoes disponiveis para selecao.</param>
    /// <returns>Opcao selecionada ou nulo quando o utilizador cancela.</returns>
    public async Task<string?> ShowSelectionAsync(string title, string cancel, params string[] options)
    {
        var selection = await GetCurrentPage().DisplayActionSheet(
            title,
            cancel,
            null,
            options);

        return selection == cancel ? null : selection;
    }

    /// <summary>
    /// Apresenta um dialog de prompt textual.
    /// </summary>
    /// <param name="title">Titulo do dialog.</param>
    /// <param name="message">Mensagem explicativa do input esperado.</param>
    /// <param name="options">Configuracao opcional do prompt.</param>
    /// <returns>Texto introduzido pelo utilizador ou nulo quando cancela.</returns>
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

    /// <summary>
    /// Pede confirmacao binaria ao utilizador.
    /// </summary>
    /// <param name="title">Titulo do dialog.</param>
    /// <param name="message">Mensagem de confirmacao.</param>
    /// <param name="accept">Texto do botao positivo.</param>
    /// <param name="cancel">Texto do botao negativo.</param>
    /// <returns>True quando o utilizador confirma a operacao.</returns>
    public async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        return await GetCurrentPage().DisplayAlert(title, message, accept, cancel);
    }

    /// <summary>
    /// Pede confirmacao explicita para uma operacao de eliminacao.
    /// </summary>
    /// <param name="message">Descricao do elemento a eliminar.</param>
    /// <returns>True quando o utilizador confirma a eliminacao.</returns>
    public async Task<bool> ConfirmDeleteAsync(string message)
    {
        return await ConfirmAsync(
            "Confirmar eliminacao",
            $"Tem a certeza que pretende eliminar {message}?",
            "Eliminar",
            "Cancelar");
    }

    /// <summary>
    /// Mostra uma mensagem informativa.
    /// </summary>
    /// <param name="title">Titulo do dialog.</param>
    /// <param name="message">Mensagem a apresentar.</param>
    /// <returns>Tarefa assincrona do dialog.</returns>
    public async Task ShowInfoAsync(string title, string message)
    {
        await GetCurrentPage().DisplayAlert(title, message, "OK");
    }

    /// <summary>
    /// Mostra uma mensagem de sucesso.
    /// </summary>
    /// <param name="title">Titulo do dialog.</param>
    /// <param name="message">Mensagem a apresentar.</param>
    /// <returns>Tarefa assincrona do dialog.</returns>
    public async Task ShowSuccessAsync(string title, string message)
    {
        await GetCurrentPage().DisplayAlert(title, message, "OK");
    }

    /// <summary>
    /// Mostra uma mensagem de erro.
    /// </summary>
    /// <param name="title">Titulo do dialog.</param>
    /// <param name="message">Mensagem a apresentar.</param>
    /// <returns>Tarefa assincrona do dialog.</returns>
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
