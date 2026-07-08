using TipMolde.Diagnostics;
using TipMolde.Helper;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class ClienteDetalhePage : ContentPage, IQueryAttributable
{
    private readonly ClienteDetalheViewModel _viewModel;

    public ClienteDetalhePage(ClienteDetalheViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.EnsureDefaultEstadoFilter();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("cliente_id", out var rawValue))
            return;

        TaskMonitor.Observe("TipMolde.View.ClienteDetalhePage.ApplyQueryAttributes", CarregarClienteAsync(rawValue));
    }

    private async Task CarregarClienteAsync(object rawValue)
    {
        var cliente_id = QueryAttributeHelper.ParseInt(rawValue);

        if (cliente_id.HasValue)
            await _viewModel.LoadAsync(cliente_id.Value);
    }
}
