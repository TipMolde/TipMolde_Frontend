using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class TopBarViewModel : ObservableObject
{
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly UtilizadoresService _utilizadoresService;
    private bool _isLoaded;

    public TopBarViewModel(
        SessaoPersistidaService sessaoPersistidaService,
        UtilizadoresService utilizadoresService)
    {
        _sessaoPersistidaService = sessaoPersistidaService;
        _utilizadoresService = utilizadoresService;
    }

    [ObservableProperty]
    private string currentUserName = "Utilizador";

    public string DisplayUserName =>
        DeviceInfo.Current.Idiom == DeviceIdiom.Phone
            ? GetFirstName(CurrentUserName)
            : CurrentUserName;

    partial void OnCurrentUserNameChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayUserName));
    }

    public async Task EnsureLoadedAsync()
    {
        if (_isLoaded)
            return;

        _isLoaded = true;

        var currentUserId = _sessaoPersistidaService.TryGetCurrentUserId();
        if (currentUserId is null)
        {
            CurrentUserName = "Utilizador";
            return;
        }

        try
        {
            var utilizador = await _utilizadoresService.GetUtilizadorByIdAsync(currentUserId.Value);
            CurrentUserName = string.IsNullOrWhiteSpace(utilizador.Nome)
                ? "Utilizador"
                : utilizador.Nome;
        }
        catch
        {
            CurrentUserName = "Utilizador";
        }
    }

    [RelayCommand]
    private async Task Logout()
    {
        await _sessaoPersistidaService.ClearSessionAsync();
        await Shell.Current.GoToAsync("//AutenticacaoPage");
    }

    private static string GetFirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "Utilizador";

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : fullName;
    }
}