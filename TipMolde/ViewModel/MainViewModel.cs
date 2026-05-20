using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TipMolde.Services;
using TipMolde.View;

namespace TipMolde.ViewModel;

public partial class MainViewModel : ObservableObject
{

    private readonly ApiConnectivityService _apiConnectivityService;
    private bool _hasCheckedOnLoad;

    public MainViewModel(ApiConnectivityService apiConnectivityService)
    {
        _apiConnectivityService = apiConnectivityService;
        EndpointMessage = "Endpoint: por resolver";
        StatusMessage = "A verificar ligacao...";
        DetailsMessage = "A app vai testar automaticamente o endpoint da API ao abrir.";
    }

    [RelayCommand]
    private async Task EnsureInitialLoadAsync()
    {
        if (_hasCheckedOnLoad)
            return;

        _hasCheckedOnLoad = true;
        await TestConnectionAsync();
    }

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string detailsMessage = string.Empty;

    [ObservableProperty]
    private string endpointMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public bool CanTestConnection => !IsBusy;

    [RelayCommand]
    private async Task OnTestConnectionClicked()
    {
        await TestConnectionAsync();
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        EndpointMessage = $"Endpoint: {_apiConnectivityService.BaseUrl}";

        try
        {
            var result = await _apiConnectivityService.CheckHealthAsync();

            if (result.IsSuccess)
            {
                StatusMessage = "Ligacao estabelecida com sucesso.";
                DetailsMessage = result.TimestampUtc is null
                    ? result.Message
                    : $"{result.Message} UTC {result.TimestampUtc:dd/MM/yyyy HH:mm:ss}.";
            }
            else
            {
                StatusMessage = "Nao foi possivel ligar ao backend.";
                DetailsMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Erro ao testar ligacao.";
            DetailsMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanTestConnection));
        }
    }
}
