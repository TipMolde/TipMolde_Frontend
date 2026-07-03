using TipMolde.Diagnostics;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class MaquinaDetalhePage : ContentPage, IQueryAttributable
{
    private readonly MaquinaDetalheViewModel _viewModel;
    private int? _lastMaquinaId;

    public MaquinaDetalhePage(MaquinaDetalheViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("maquina_id", out var rawMaquinaId))
            return;

        var maquinaId = rawMaquinaId switch
        {
            int value => value,
            string value when int.TryParse(value, out var parsed) => parsed,
            _ => 0
        };

        if (maquinaId <= 0 || _lastMaquinaId == maquinaId)
            return;

        _lastMaquinaId = maquinaId;
        TaskMonitor.Observe("TipMolde.View.MaquinaDetalhePage.ApplyQueryAttributes", _viewModel.LoadAsync(maquinaId));
    }
}
