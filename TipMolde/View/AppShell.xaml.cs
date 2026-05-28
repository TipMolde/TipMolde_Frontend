namespace TipMolde.View;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(AutenticacaoPage), typeof(AutenticacaoPage));
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(Utilizadores), typeof(Utilizadores));
        Routing.RegisterRoute(nameof(AdicionarUtilizadorPage), typeof(AdicionarUtilizadorPage));
        Routing.RegisterRoute(nameof(Clientes), typeof(Clientes));
        Routing.RegisterRoute(nameof(AdicionarClientePage), typeof(AdicionarClientePage));
        Routing.RegisterRoute(nameof(EditarClientePage), typeof(EditarClientePage));
        Routing.RegisterRoute(nameof(ClienteDetalhePage), typeof(ClienteDetalhePage));
        Routing.RegisterRoute(nameof(Encomendas), typeof(Encomendas));
        Routing.RegisterRoute(nameof(Producao), typeof(Producao));
        Routing.RegisterRoute(nameof(Definicoes), typeof(Definicoes));
    }
}
