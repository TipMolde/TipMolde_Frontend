using TipMolde.Diagnostics;
using TipMolde.Helper;
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
        TaskMonitor.Observe("TipMolde.View.EncomendaDetalhePage.ApplyQueryAttributes", CarregarEncomendaAsync(rawValue));
    }

    private async Task CarregarEncomendaAsync(object rawValue)
    {
        var encomendaId = QueryAttributeHelper.ParseInt(rawValue);

        if (!encomendaId.HasValue || _lastEncomendaId == encomendaId.Value)
            return;

        _lastEncomendaId = encomendaId.Value;
        await _viewModel.LoadAsync(encomendaId.Value);
    }
}
