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

            builder.Services.AddSingleton(_ => ApiEndpointResolver.Resolve());

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
            builder.Services.AddSingleton<ThemePreferenceService>();
            builder.Services.AddSingleton<UtilizadoresService>();
            builder.Services.AddSingleton<AuthorizationService>();
            builder.Services.AddSingleton<IDialogService, DialogService>();
            builder.Services.AddSingleton<IDestinationFolderPickerService, DestinationFolderPickerService>();
            builder.Services.AddSingleton<AppShell>();

            builder.Services.AddSingleton<TopBarViewModel>();
            builder.Services.AddTransient<SidebarViewModel>();

            builder.Services.AddTransient<AutenticacaoService>();
            builder.Services.AddTransient<AutenticacaoPage>();
            builder.Services.AddTransient<AutenticacaoViewModel>();

            builder.Services.AddSingleton<DashboardViewModel>();
            builder.Services.AddSingleton<MainPage>();

            builder.Services.AddTransient<UtilizadoresViewModel>();
            builder.Services.AddTransient<Utilizadores>();

            builder.Services.AddTransient<AdicionarUtilizadorViewModel>();
            builder.Services.AddTransient<AdicionarUtilizadorPage>();

            builder.Services.AddTransient<ClientesService>();
            builder.Services.AddTransient<ClientesViewModel>();
            builder.Services.AddTransient<Clientes>();

            builder.Services.AddTransient<EncomendasService>();
            builder.Services.AddTransient<FornecedoresService>();
            builder.Services.AddTransient<PedidosMaterialService>();
            builder.Services.AddTransient<MoldesService>();
            builder.Services.AddTransient<ProjetosService>();
            builder.Services.AddTransient<RevisoesService>();
            builder.Services.AddTransient<RegistosTempoProjetoService>();
            builder.Services.AddTransient<PecasService>();
            builder.Services.AddTransient<MaquinasService>();
            builder.Services.AddTransient<FasesProducaoService>();
            builder.Services.AddTransient<RegistosProducaoService>();
            builder.Services.AddTransient<GlobalMoldePriorityService>();
            builder.Services.AddTransient<RelatoriosService>();
            builder.Services.AddTransient<ProducaoViewModelDependencies>();
            builder.Services.AddTransient<RegistoProducaoViewModelDependencies>();

            builder.Services.AddTransient<AdicionarClienteViewModel>();
            builder.Services.AddTransient<AdicionarClientePage>();

            builder.Services.AddTransient<AdicionarEncomendaViewModel>();
            builder.Services.AddTransient<AdicionarEncomendaPage>();

            builder.Services.AddTransient<AdicionarMoldeViewModel>();
            builder.Services.AddTransient<AdicionarMoldePage>();
            builder.Services.AddTransient<EditarMoldeViewModel>();
            builder.Services.AddTransient<EditarMoldePage>();
            builder.Services.AddTransient<AdicionarPecaViewModel>();
            builder.Services.AddTransient<AdicionarPecaPage>();
            builder.Services.AddTransient<EditarPecaViewModel>();
            builder.Services.AddTransient<EditarPecaPage>();
            builder.Services.AddTransient<EditarMaquinaViewModel>();
            builder.Services.AddTransient<EditarMaquinaPage>();

            builder.Services.AddTransient<EditarClienteViewModel>();
            builder.Services.AddTransient<EditarClientePage>();

            builder.Services.AddTransient<ClienteDetalheViewModel>();
            builder.Services.AddTransient<ClienteDetalhePage>();

            builder.Services.AddTransient<EncomendasViewModel>();
            builder.Services.AddTransient<Encomendas>();

            builder.Services.AddTransient<PedidosMaterialViewModel>();
            builder.Services.AddTransient<PedidosMaterial>();

            builder.Services.AddTransient<EncomendaDetalheViewModel>();
            builder.Services.AddTransient<EncomendaDetalhePage>();

            builder.Services.AddTransient<MoldeDetalheViewModel>();
            builder.Services.AddTransient<MoldeDetalhePage>();

            builder.Services.AddTransient<ProducaoViewModel>();
            builder.Services.AddTransient<Producao>();
            builder.Services.AddTransient<FopGeralViewModel>();
            builder.Services.AddTransient<FopGeralPage>();
            builder.Services.AddTransient<RegistoProducaoViewModel>();
            builder.Services.AddTransient<RegistoProducaoPage>();
            builder.Services.AddTransient<FilaTrabalhoViewModel>();
            builder.Services.AddTransient<FilaTrabalhoPage>();
            builder.Services.AddTransient<MaquinasViewModel>();
            builder.Services.AddTransient<MaquinasPage>();
            builder.Services.AddTransient<DesenhoViewModel>();
            builder.Services.AddTransient<DesenhoPage>();
            builder.Services.AddTransient<ProjetosViewModel>();
            builder.Services.AddTransient<ProjetosPage>();
            builder.Services.AddTransient<AdicionarProjetoViewModel>();
            builder.Services.AddTransient<AdicionarProjetoPage>();
            builder.Services.AddTransient<ProjetoDetalheViewModel>();
            builder.Services.AddTransient<ProjetoDetalhePage>();
            builder.Services.AddTransient<RelatoriosViewModel>();
            builder.Services.AddTransient<RelatoriosPage>();

            builder.Services.AddTransient<DefinicoesViewModel>();
            builder.Services.AddTransient<Definicoes>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
