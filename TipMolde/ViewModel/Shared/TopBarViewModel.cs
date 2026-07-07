using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using TipMolde.Services;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere a barra superior da aplicacao autenticada.
/// </summary>
public partial class TopBarViewModel : ObservableObject
{
    private const string DefaultUserName = "Utilizador";

    private readonly AuthorizationService _authorizationService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly UtilizadoresService _utilizadoresService;
    private readonly INavigationService _navigationService;
    private readonly ResponsiveLayoutService _responsiveLayoutService;
    private bool _isLoaded;

    /// <summary>
    /// Construtor do view model da top bar.
    /// </summary>
    /// <param name="authorizationService">Servico usado para limpar a role no logout.</param>
    /// <param name="sessaoPersistidaService">Servico usado para obter e limpar a sessao atual.</param>
    /// <param name="utilizadoresService">Servico usado para resolver o nome do utilizador autenticado.</param>
    /// <param name="navigationService">Servico de navegacao principal da aplicacao.</param>
    /// <param name="responsiveLayoutService">Servico responsivo usado para ajustar o nome apresentado.</param>
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

    /// <summary>
    /// Carrega o nome do utilizador autenticado para a barra superior.
    /// </summary>
    /// <param name="forceRefresh">Indica se o carregamento deve ignorar o estado local atual.</param>
    /// <returns>Tarefa assincrona da atualizacao da top bar.</returns>
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

    /// <summary>
    /// Limpa o estado visual da top bar para reutilizacao apos logout.
    /// </summary>
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

    /// <summary>
    /// Abre o menu de navegacao quando o layout atual o permite.
    /// </summary>
    /// <returns>Tarefa concluida apos o pedido de abertura do menu.</returns>
    [RelayCommand]
    private Task OpenNavigationMenuAsync()
    {
        _navigationService.OpenFlyout();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Termina a sessao atual e encaminha o utilizador para autenticacao.
    /// </summary>
    /// <returns>Tarefa assincrona do fluxo de logout.</returns>
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
