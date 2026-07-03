using TipMolde.Diagnostics;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class PedidosMaterial : ContentPage
{
    private readonly PedidosMaterialViewModel _viewModel;

    public PedidosMaterial(PedidosMaterialViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TaskMonitor.Observe("TipMolde.View.PedidosMaterial.OnAppearing", OnAppearingAsync());
    }

    private async Task OnAppearingAsync()
    {
        await _viewModel.LoadAsync();
    }
}
