using Microsoft.Extensions.Logging;

using TipMolde.Configuration;
using TipMolde.Services;
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

            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<MainPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
