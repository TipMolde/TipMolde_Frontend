using TipMolde.Diagnostics;
using TipMolde.Helper;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class ProjetoDetalhePage : ContentPage, IQueryAttributable
{
    private readonly ProjetoDetalheViewModel _viewModel;
    private int? _lastProjetoId;

    public ProjetoDetalhePage(ProjetoDetalheViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("projeto_id", out var rawProjetoId))
            return;

        query.Clear();
        TaskMonitor.Observe("TipMolde.View.ProjetoDetalhePage.ApplyQueryAttributes", CarregarContextoAsync(rawProjetoId));
    }

    private async Task CarregarContextoAsync(object rawProjetoId)
    {
        var projetoId = QueryAttributeHelper.ParseInt(rawProjetoId, allowUriDecoding: true);

        if (!projetoId.HasValue || _lastProjetoId == projetoId.Value)
            return;

        _lastProjetoId = projetoId.Value;
        await _viewModel.LoadAsync(projetoId.Value);
    }
}
