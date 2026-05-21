using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class AutenticacaoViewModel : ObservableObject
{
    private readonly ApiConnectivityService _apiConnectivityService;
    private readonly AutenticacaoService _autenticacaoService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private bool _hasCheckedOnLoad;

    public AutenticacaoViewModel(
        ApiConnectivityService apiConnectivityService,
        AutenticacaoService autenticacaoService,
        SessaoPersistidaService sessaoPersistidaService)
    {
        _apiConnectivityService = apiConnectivityService;
        _autenticacaoService = autenticacaoService;
        _sessaoPersistidaService = sessaoPersistidaService;
        RememberSession = _sessaoPersistidaService.ShouldRememberSession;
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
    private bool isBusy;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
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
        if (IsBusy)
            return;

        ClearError();

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ShowError(
                "Credenciais em falta",
                "Indique um email valido e a respetiva palavra-passe antes de continuar.");
            return;
        }

        IsBusy = true;

        try
        {
            var result = await _autenticacaoService.LoginAsync(Email.Trim(), Password);
            await _sessaoPersistidaService.SaveSessionAsync(result.Token, result.ExpiresAt, RememberSession);
            Password = string.Empty;
            ClearError();

            await Shell.Current.GoToAsync("//MainPage");
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
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ClearError();

        try
        {
            var result = await _apiConnectivityService.CheckHealthAsync();

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
            IsBusy = false;
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
