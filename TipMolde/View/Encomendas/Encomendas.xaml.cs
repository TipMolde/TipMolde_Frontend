using TipMolde.Diagnostics;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class Encomendas : ContentPage
{
    private readonly EncomendasViewModel _viewModel;

    public Encomendas(EncomendasViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TaskMonitor.Observe("TipMolde.View.Encomendas.OnAppearing", OnAppearingAsync());
    }

    private async Task OnAppearingAsync()
    {
        await _viewModel.LoadEncomendasAsync();
    }
}
