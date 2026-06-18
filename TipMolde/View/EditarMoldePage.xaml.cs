using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class EditarMoldePage : ContentPage, IQueryAttributable
{
    private readonly EditarMoldeViewModel _viewModel;
    private int? _lastMoldeId;
    private bool _hasLoadedOnce;

    public EditarMoldePage(EditarMoldeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("molde_id", out var rawValue))
            return;

        query.Clear();
        _ = CarregarMoldeAsync(rawValue);
    }

    private async Task CarregarMoldeAsync(object rawValue)
    {
        int? moldeId = rawValue switch
        {
            int id => id,
            string text when int.TryParse(text, out var parsedId) => parsedId,
            _ => null
        };

        if (!moldeId.HasValue || _lastMoldeId == moldeId.Value)
            return;

        _lastMoldeId = moldeId.Value;
        await _viewModel.LoadAsync(moldeId.Value);
        _hasLoadedOnce = true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_hasLoadedOnce || !_lastMoldeId.HasValue)
            return;

        await _viewModel.LoadAsync(_lastMoldeId.Value);
    }
}
