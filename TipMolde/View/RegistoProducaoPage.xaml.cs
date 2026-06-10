using TipMolde.Models;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class RegistoProducaoPage : ContentPage, IQueryAttributable
{
    private readonly RegistoProducaoViewModel _viewModel;

    public RegistoProducaoPage(RegistoProducaoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        query.TryGetValue("peca_contexto", out var rawContext);
        query.Clear();

        var contexto = rawContext as ProducaoPecaDisponivelItem;
        _ = _viewModel.LoadAsync(contexto);
    }
}
