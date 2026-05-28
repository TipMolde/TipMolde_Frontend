using Microsoft.Extensions.Logging;
using TipMolde.Configuration;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel;

namespace TipMolde
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton(new ApiOptions
            {
                BaseUrl = ApiEndpointResolver.GetDefaultBaseUrl()
            });

            builder.Services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<ApiOptions>();

                return new HttpClient
                {
                    BaseAddress = new Uri(options.BaseUrl),
                    Timeout = TimeSpan.FromSeconds(15)
                };
            });

            builder.Services.AddSingleton<ApiConnectivityService>();
            builder.Services.AddSingleton<SessaoPersistidaService>();
            builder.Services.AddSingleton<IDialogService, DialogService>();
            builder.Services.AddSingleton<AppShell>();

            builder.Services.AddSingleton<TopBarViewModel>();

            builder.Services.AddTransient<AutenticacaoService>();
            builder.Services.AddTransient<AutenticacaoPage>();
            builder.Services.AddTransient<AutenticacaoViewModel>();

            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<MainPage>();

            builder.Services.AddTransient<UtilizadoresService>();
            builder.Services.AddTransient<UtilizadoresViewModel>();
            builder.Services.AddTransient<Utilizadores>();

            builder.Services.AddTransient<AdicionarUtilizadorViewModel>();
            builder.Services.AddTransient<AdicionarUtilizadorPage>();

            builder.Services.AddTransient<ClientesService>();
            builder.Services.AddTransient<ClientesViewModel>();
            builder.Services.AddTransient<Clientes>();

            builder.Services.AddTransient<AdicionarClienteViewModel>();
            builder.Services.AddTransient<AdicionarClientePage>();

            builder.Services.AddTransient<EditarClienteViewModel>();
            builder.Services.AddTransient<EditarClientePage>();

            builder.Services.AddTransient<ClienteDetalheViewModel>();
            builder.Services.AddTransient<ClienteDetalhePage>();

            builder.Services.AddTransient<EncomendasViewModel>();
            builder.Services.AddTransient<Encomendas>();

            builder.Services.AddTransient<ProducaoViewModel>();
            builder.Services.AddTransient<Producao>();

            builder.Services.AddTransient<DefinicoesViewModel>();
            builder.Services.AddTransient<Definicoes>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
