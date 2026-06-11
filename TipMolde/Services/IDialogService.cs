namespace TipMolde.Services;

public interface IDialogService
{
    Page GetCurrentPage();
    Task<string> ShowOptionsAsync(string message, string action);
    Task<string?> ShowSelectionAsync(string title, string cancel, params string[] options);
    Task<string?> PromptAsync(string title, string message, PromptDialogOptions? options = null);
    Task<bool> ConfirmDeleteAsync(string message);
    Task ShowInfoAsync(string title, string message);
    Task ShowSuccessAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
}

public sealed record PromptDialogOptions
{
    public string Accept { get; init; } = "OK";
    public string Cancel { get; init; } = "Cancelar";
    public string InitialValue { get; init; } = string.Empty;
    public int MaxLength { get; init; } = -1;
    public Keyboard? Keyboard { get; init; }
    public string Placeholder { get; init; } = string.Empty;
}
