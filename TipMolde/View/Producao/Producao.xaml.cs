using TipMolde.Diagnostics;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class Producao : ContentPage
{
    private readonly ProducaoViewModel _viewModel;

    public Producao(ProducaoViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TaskMonitor.Observe("TipMolde.View.Producao.OnAppearing", OnAppearingAsync());
    }

    private async Task OnAppearingAsync()
    {
        await _viewModel.LoadAsync();
    }
}
