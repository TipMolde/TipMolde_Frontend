using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class AdicionarPecaPage : ContentPage, IQueryAttributable
{
    private readonly AdicionarPecaViewModel _viewModel;
    private int? _lastMoldeId;

    public AdicionarPecaPage(AdicionarPecaViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("molde_id", out var rawMoldeId))
            return;

        query.TryGetValue("numero_molde", out var rawNumeroMolde);
        query.Clear();
        _ = CarregarContextoAsync(rawMoldeId, rawNumeroMolde);
    }

    private async Task CarregarContextoAsync(object rawMoldeId, object? rawNumeroMolde)
    {
        int? moldeId = rawMoldeId switch
        {
            int id => id,
            string text when int.TryParse(text, out var parsedId) => parsedId,
            _ => null
        };

        if (!moldeId.HasValue || _lastMoldeId == moldeId.Value)
            return;

        _lastMoldeId = moldeId.Value;
        var numeroMolde = rawNumeroMolde?.ToString();
        await _viewModel.LoadAsync(moldeId.Value, numeroMolde);
    }
}
