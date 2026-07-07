namespace TipMolde.Services;

/// <summary>
/// Abstrai dialogs modais usados pelos view models do frontend.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Mostra uma folha de acoes simples.
    /// </summary>
    /// <param name="message">Descricao do contexto da acao.</param>
    /// <param name="action">Acao principal apresentada.</param>
    /// <returns>Texto da opcao selecionada.</returns>
    Task<string> ShowOptionsAsync(string message, string action);

    /// <summary>
    /// Mostra uma lista de opcoes mutuamente exclusivas.
    /// </summary>
    /// <param name="title">Titulo do dialog.</param>
    /// <param name="cancel">Texto da opcao de cancelamento.</param>
    /// <param name="options">Opcoes disponiveis.</param>
    /// <returns>Opcao selecionada ou nulo quando o utilizador cancela.</returns>
    Task<string?> ShowSelectionAsync(string title, string cancel, params string[] options);

    /// <summary>
    /// Mostra um prompt para recolha de texto.
    /// </summary>
    /// <param name="title">Titulo do dialog.</param>
    /// <param name="message">Mensagem do prompt.</param>
    /// <param name="options">Configuracao opcional do prompt.</param>
    /// <returns>Texto introduzido ou nulo quando o utilizador cancela.</returns>
    Task<string?> PromptAsync(string title, string message, PromptDialogOptions? options = null);

    /// <summary>
    /// Mostra um dialog de confirmacao.
    /// </summary>
    /// <param name="title">Titulo do dialog.</param>
    /// <param name="message">Mensagem da confirmacao.</param>
    /// <param name="accept">Texto do botao positivo.</param>
    /// <param name="cancel">Texto do botao negativo.</param>
    /// <returns>True quando o utilizador confirma.</returns>
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);

    /// <summary>
    /// Mostra um dialog de confirmacao orientado para eliminacao.
    /// </summary>
    /// <param name="message">Descricao do elemento a eliminar.</param>
    /// <returns>True quando o utilizador confirma a remocao.</returns>
    Task<bool> ConfirmDeleteAsync(string message);

    /// <summary>
    /// Mostra uma mensagem informativa.
    /// </summary>
    /// <param name="title">Titulo do dialog.</param>
    /// <param name="message">Mensagem a apresentar.</param>
    /// <returns>Tarefa assincrona do dialog.</returns>
    Task ShowInfoAsync(string title, string message);

    /// <summary>
    /// Mostra uma mensagem de sucesso.
    /// </summary>
    /// <param name="title">Titulo do dialog.</param>
    /// <param name="message">Mensagem a apresentar.</param>
    /// <returns>Tarefa assincrona do dialog.</returns>
    Task ShowSuccessAsync(string title, string message);

    /// <summary>
    /// Mostra uma mensagem de erro.
    /// </summary>
    /// <param name="title">Titulo do dialog.</param>
    /// <param name="message">Mensagem a apresentar.</param>
    /// <returns>Tarefa assincrona do dialog.</returns>
    Task ShowErrorAsync(string title, string message);
}

/// <summary>
/// Agrupa opcoes de configuracao para prompts textuais.
/// </summary>
public sealed record PromptDialogOptions
{
    public string Accept { get; init; } = "OK";
    public string Cancel { get; init; } = "Cancelar";
    public string InitialValue { get; init; } = string.Empty;
    public int MaxLength { get; init; } = -1;
    public Keyboard? Keyboard { get; init; }
    public string Placeholder { get; init; } = string.Empty;
}
