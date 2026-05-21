namespace TipMolde.Services;

public interface IDialogService
{
    Page GetCurrentPage();
    Task<string> ShowOptionsAsync(string message, string action);
    Task<bool> ConfirmDeleteAsync(string message);
    Task ShowInfoAsync(string title, string message);
    Task ShowSuccessAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
}
