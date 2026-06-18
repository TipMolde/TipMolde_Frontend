using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class TopBarViewModel : ObservableObject
{
    private const string DefaultUserName = "Utilizador";

    private readonly AuthorizationService _authorizationService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly UtilizadoresService _utilizadoresService;
    private bool _isLoaded;

    public TopBarViewModel(
        AuthorizationService authorizationService,
        SessaoPersistidaService sessaoPersistidaService,
        UtilizadoresService utilizadoresService)
    {
        _authorizationService = authorizationService;
        _sessaoPersistidaService = sessaoPersistidaService;
        _utilizadoresService = utilizadoresService;
    }

    [ObservableProperty]
    private string currentUserName = DefaultUserName;

    public string DisplayUserName =>
        DeviceInfo.Current.Idiom == DeviceIdiom.Phone
            ? GetFirstName(CurrentUserName)
            : CurrentUserName;

    public static bool ShowNavigationMenu => DeviceInfo.Current.Idiom == DeviceIdiom.Phone;

    partial void OnCurrentUserNameChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayUserName));
    }

    public async Task EnsureLoadedAsync(bool forceRefresh = false)
    {
        if (_isLoaded && !forceRefresh)
            return;

        _isLoaded = true;

        var currentUserId = _sessaoPersistidaService.TryGetCurrentUserId();
        if (currentUserId is null)
        {
            CurrentUserName = DefaultUserName;
            return;
        }

        try
        {
            var utilizador = await _utilizadoresService.GetUtilizadorByIdAsync(currentUserId.Value);
            CurrentUserName = string.IsNullOrWhiteSpace(utilizador.Nome)
                ? DefaultUserName
                : utilizador.Nome;
        }
        catch
        {
            CurrentUserName = DefaultUserName;
        }
    }

    public void Reset()
    {
        _isLoaded = false;
        CurrentUserName = DefaultUserName;
    }

    [RelayCommand]
    private static async Task OpenDefinicoes()
    {
        await Shell.Current.GoToAsync("//Definicoes");
    }

    [RelayCommand]
    private static Task OpenNavigationMenuAsync()
    {
        if (Shell.Current is not null)
            Shell.Current.FlyoutIsPresented = true;

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Logout()
    {
        Reset();
        _authorizationService.Clear();
        await _sessaoPersistidaService.ClearSessionAsync();
        await Shell.Current.GoToAsync("//AutenticacaoPage");
    }

    private static string GetFirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return DefaultUserName;

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : fullName;
    }
}
