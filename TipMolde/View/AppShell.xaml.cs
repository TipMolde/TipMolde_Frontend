namespace TipMolde.View;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(AdicionarUtilizadorPage), typeof(AdicionarUtilizadorPage));
        Routing.RegisterRoute(nameof(AdicionarClientePage), typeof(AdicionarClientePage));
        Routing.RegisterRoute(nameof(AdicionarEncomendaPage), typeof(AdicionarEncomendaPage));
        Routing.RegisterRoute(nameof(EditarClientePage), typeof(EditarClientePage));
        Routing.RegisterRoute(nameof(ClienteDetalhePage), typeof(ClienteDetalhePage));
        Routing.RegisterRoute(nameof(EncomendaDetalhePage), typeof(EncomendaDetalhePage));
        Routing.RegisterRoute(nameof(MoldeDetalhePage), typeof(MoldeDetalhePage));
    }
}
