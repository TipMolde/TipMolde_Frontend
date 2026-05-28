using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class EncomendaDetalhePage : ContentPage, IQueryAttributable
{
    private readonly EncomendaDetalheViewModel _viewModel;
    private int? _lastEncomendaId;

    public EncomendaDetalhePage(EncomendaDetalheViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("encomenda_id", out var rawValue))
            return;

        query.Clear();
        _ = CarregarEncomendaAsync(rawValue);
    }

    private async Task CarregarEncomendaAsync(object rawValue)
    {
        int? encomendaId = rawValue switch
        {
            int id => id,
            string text when int.TryParse(text, out var parsedId) => parsedId,
            _ => null
        };

        if (!encomendaId.HasValue || _lastEncomendaId == encomendaId.Value)
            return;

        _lastEncomendaId = encomendaId.Value;
        await _viewModel.LoadAsync(encomendaId.Value);
    }
}
