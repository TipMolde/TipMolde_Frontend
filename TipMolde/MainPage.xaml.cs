using Microsoft.Extensions.DependencyInjection;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde
{
    public partial class MainPage : ContentPage
    {
        private ApiConnectivityService? _apiConnectivityService;
        private bool _hasCheckedOnLoad;

        public MainPage(MainViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_hasCheckedOnLoad)
                return;

            _hasCheckedOnLoad = true;
            await TestConnectionAsync();
        }

        private async void OnTestConnectionClicked(object sender, EventArgs e)
        {
            await TestConnectionAsync();
        }

        private async Task TestConnectionAsync()
        {
            ResolveApiConnectivityService();

            if (_apiConnectivityService is null)
            {
                StatusLabel.Text = "Servico de API indisponivel.";
                DetailsLabel.Text = "O frontend nao conseguiu resolver o cliente HTTP configurado.";
                EndpointLabel.Text = "Endpoint: nao disponivel";
                return;
            }

            SetBusyState(true);
            EndpointLabel.Text = $"Endpoint: {_apiConnectivityService.BaseUrl}";

            var result = await _apiConnectivityService.CheckHealthAsync();

            if (result.IsSuccess)
            {
                StatusLabel.Text = "Ligacao estabelecida com sucesso.";
                DetailsLabel.Text = result.TimestampUtc is null
                    ? result.Message
                    : $"{result.Message} UTC {result.TimestampUtc:dd/MM/yyyy HH:mm:ss}.";
            }
            else
            {
                StatusLabel.Text = "Nao foi possivel ligar ao backend.";
                DetailsLabel.Text = result.Message;
            }

            SetBusyState(false);
        }

        private void ResolveApiConnectivityService()
        {
            if (_apiConnectivityService is not null)
                return;

            _apiConnectivityService =
                Handler?.MauiContext?.Services.GetService<ApiConnectivityService>()
                ?? Application.Current?.Handler?.MauiContext?.Services.GetService<ApiConnectivityService>();
        }

        private void SetBusyState(bool isBusy)
        {
            ConnectionActivityIndicator.IsVisible = isBusy;
            ConnectionActivityIndicator.IsRunning = isBusy;
            TestConnectionButton.IsEnabled = !isBusy;
        }
    }
}
