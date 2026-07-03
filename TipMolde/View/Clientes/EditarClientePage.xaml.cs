using TipMolde.Diagnostics;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class EditarClientePage : ContentPage, IQueryAttributable
{
    private readonly EditarClienteViewModel _viewModel;

    public EditarClientePage(EditarClienteViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("cliente_id", out var rawValue))
            return;

        TaskMonitor.Observe("TipMolde.View.EditarClientePage.ApplyQueryAttributes", CarregarClienteAsync(rawValue));
    }

    private async Task CarregarClienteAsync(object rawValue)
    {
        int? cliente_id = rawValue switch
        {
            int id => id,
            string text when int.TryParse(text, out var parsedId) => parsedId,
            _ => null
        };

        if (cliente_id.HasValue)
            await _viewModel.LoadAsync(cliente_id.Value);
    }
}
