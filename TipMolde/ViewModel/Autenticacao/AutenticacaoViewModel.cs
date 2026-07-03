using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class AutenticacaoViewModel : ObservableObject
{
    private readonly ApiConnectivityService _apiConnectivityService;
    private readonly AutenticacaoService _autenticacaoService;
    private readonly AuthorizationService _authorizationService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly INavigationService _navigationService;
    private bool _hasCheckedOnLoad;

    public AutenticacaoViewModel(
        ApiConnectivityService apiConnectivityService,
        AutenticacaoService autenticacaoService,
        AuthorizationService authorizationService,
        SessaoPersistidaService sessaoPersistidaService,
        INavigationService navigationService)
    {
        _apiConnectivityService = apiConnectivityService;
        _autenticacaoService = autenticacaoService;
        _authorizationService = authorizationService;
        _sessaoPersistidaService = sessaoPersistidaService;
        _navigationService = navigationService;
        RememberSession = SessaoPersistidaService.ShouldRememberSession;
    }

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool rememberSession;

    [ObservableProperty]
    private string errorTitle = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isLoggingIn;

    [ObservableProperty]
    private bool isCheckingConnection;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsBusy => IsLoggingIn || IsCheckingConnection;

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnIsLoggingInChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBusy));
    }

    partial void OnIsCheckingConnectionChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBusy));
    }

    [RelayCommand]
    private async Task EnsureInitialLoadAsync()
    {
        if (_hasCheckedOnLoad)
            return;

        _hasCheckedOnLoad = true;
        await TestConnectionAsync();
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsLoggingIn)
            return;

        ClearError();

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ShowError(
                "Credenciais em falta",
                "Indique um email valido e a respetiva palavra-passe antes de continuar.");
            return;
        }

        IsLoggingIn = true;

        try
        {
            var result = await _autenticacaoService.LoginAsync(Email.Trim(), Password);
            await _sessaoPersistidaService.SaveSessionAsync(result.Token, result.ExpiresAt, RememberSession);
            _authorizationService.Clear();

            try
            {
                await _authorizationService.GetCurrentRoleAsync(forceRefresh: true);
            }
            catch
            {
                // A sessão ja foi guardada; o papel sera recarregado quando a app voltar a precisar dele.
            }

            Password = string.Empty;
            ClearError();
            await _navigationService.NavigateToDashboardAfterLoginAsync();
        }
        catch (InvalidOperationException ex)
        {
            ShowError("Falha no login", ex.Message);
        }
        catch (Exception ex)
        {
            ShowError("Erro inesperado", ex.Message);
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (IsCheckingConnection)
            return;

        IsCheckingConnection = true;
        ClearError();

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var result = await _apiConnectivityService.CheckHealthAsync(timeout.Token);

            if (!result.IsSuccess)
            {
                ShowError(
                    "Sem ligacao a API",
                    $"{result.Message} Endpoint: {result.BaseUrl}");
            }
        }
        catch (Exception ex)
        {
            ShowError("Erro de ligacao", ex.Message);
        }
        finally
        {
            IsCheckingConnection = false;
        }
    }

    private void ShowError(string title, string message)
    {
        ErrorTitle = title;
        ErrorMessage = message;
    }

    private void ClearError()
    {
        ErrorTitle = string.Empty;
        ErrorMessage = string.Empty;
    }
}
