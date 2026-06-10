using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class EditarPecaPage : ContentPage, IQueryAttributable
{
    private readonly EditarPecaViewModel _viewModel;
    private int? _lastPecaId;

    public EditarPecaPage(EditarPecaViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("peca_id", out var rawPecaId))
            return;

        query.TryGetValue("numero_molde", out var rawNumeroMolde);
        query.Clear();
        _ = CarregarContextoAsync(rawPecaId, rawNumeroMolde);
    }

    private async Task CarregarContextoAsync(object rawPecaId, object? rawNumeroMolde)
    {
        int? pecaId = rawPecaId switch
        {
            int id => id,
            string text when int.TryParse(text, out var parsedId) => parsedId,
            _ => null
        };

        if (!pecaId.HasValue || _lastPecaId == pecaId.Value)
            return;

        _lastPecaId = pecaId.Value;
        var numeroMolde = rawNumeroMolde?.ToString();
        await _viewModel.LoadAsync(pecaId.Value, numeroMolde);
    }
}
