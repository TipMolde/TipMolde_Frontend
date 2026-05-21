using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
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

                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(options.BaseUrl),
                    Timeout = TimeSpan.FromSeconds(15)
                };

                const string jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMDEiLCJlbWFpbCI6InVzZXIxMDFAdGlwbW9sZGUudGVzdCIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6InVzZXIxMDFAdGlwbW9sZGUudGVzdCIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkFETUlOIiwianRpIjoiYTVjNTFiMjItZGQ1OC00MDFhLTkzMGUtNzcwMzdlOGJkZWM4IiwiZXhwIjoxNzc5NDA2MjM5LCJpc3MiOiJUaXBNb2xkZS5BcGkiLCJhdWQiOiJUaXBNb2xkZS5DbGllbnQifQ.zW2tg4c4ro520vS4JTTYVtyYVDlUfTr8HRxCqj5XsEA";

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtToken);

                return httpClient;
            });

            builder.Services.AddSingleton<ApiConnectivityService>();

            builder.Services.AddSingleton<AutenticacaoPage>();
            builder.Services.AddScoped<AutenticacaoViewModel>();

            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<MainPage>();

            builder.Services.AddTransient<UtilizadoresService>();
            builder.Services.AddTransient<UtilizadoresViewModel>();
            builder.Services.AddTransient<Utilizadores>();

            builder.Services.AddTransient<ClientesViewModel>();
            builder.Services.AddTransient<Clientes>();

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
