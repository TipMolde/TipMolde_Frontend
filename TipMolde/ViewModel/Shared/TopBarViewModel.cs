using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class TopBarViewModel : ObservableObject
{
    private const string DefaultUserName = "Utilizador";

    private readonly AuthorizationService _authorizationService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly UtilizadoresService _utilizadoresService;
    private readonly INavigationService _navigationService;
    private readonly ResponsiveLayoutService _responsiveLayoutService;
    private bool _isLoaded;

    public TopBarViewModel(
        AuthorizationService authorizationService,
        SessaoPersistidaService sessaoPersistidaService,
        UtilizadoresService utilizadoresService,
        INavigationService navigationService,
        ResponsiveLayoutService responsiveLayoutService)
    {
        _authorizationService = authorizationService;
        _sessaoPersistidaService = sessaoPersistidaService;
        _utilizadoresService = utilizadoresService;
        _navigationService = navigationService;
        _responsiveLayoutService = responsiveLayoutService;
        _responsiveLayoutService.PropertyChanged += OnResponsiveLayoutChanged;
    }

    [ObservableProperty]
    private string currentUserName = DefaultUserName;

    public string DisplayUserName =>
        _responsiveLayoutService.IsCompact
            ? GetFirstName(CurrentUserName)
            : CurrentUserName;

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
    private async Task OpenDefinicoes()
    {
        await _navigationService.GoToAsync("//Definicoes");
    }

    [RelayCommand]
    private Task OpenNavigationMenuAsync()
    {
        _navigationService.OpenFlyout();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Logout()
    {
        Reset();
        _authorizationService.Clear();
        await _sessaoPersistidaService.ClearSessionAsync();
        await _navigationService.NavigateToAuthenticationAsync();
    }

    private static string GetFirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return DefaultUserName;

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : fullName;
    }

    private void OnResponsiveLayoutChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.PropertyName) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.LayoutMode), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.IsCompact), StringComparison.Ordinal))
        {
            return;
        }

        OnPropertyChanged(nameof(DisplayUserName));
    }
}
