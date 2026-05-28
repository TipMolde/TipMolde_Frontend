namespace TipMolde.Services;

public interface IDialogService
{
    Page GetCurrentPage();
    Task<string> ShowOptionsAsync(string message, string action);
    Task<string?> ShowSelectionAsync(string title, string cancel, params string[] options);
    Task<string?> PromptAsync(string title, string message, string accept = "OK", string cancel = "Cancelar", string initialValue = "", int maxLength = -1, Keyboard? keyboard = null, string placeholder = "");
    Task<bool> ConfirmDeleteAsync(string message);
    Task ShowInfoAsync(string title, string message);
    Task ShowSuccessAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
}
