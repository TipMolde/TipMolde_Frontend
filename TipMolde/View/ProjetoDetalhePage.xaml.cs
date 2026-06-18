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
        _ = CarregarContextoAsync(rawProjetoId);
    }

    private async Task CarregarContextoAsync(object rawProjetoId)
    {
        int? projetoId = rawProjetoId switch
        {
            int id => id,
            string text when int.TryParse(Uri.UnescapeDataString(text), out var parsedId) => parsedId,
            string text when int.TryParse(text, out var parsedPlainId) => parsedPlainId,
            _ => null
        };

        if (!projetoId.HasValue || _lastProjetoId == projetoId.Value)
            return;

        _lastProjetoId = projetoId.Value;
        await _viewModel.LoadAsync(projetoId.Value);
    }
}
