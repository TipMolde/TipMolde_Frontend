using TipMolde.Diagnostics;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class Clientes : ContentPage
{
    private readonly ClientesViewModel _viewModel;
    public Clientes(ClientesViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TaskMonitor.Observe("TipMolde.View.Clientes.OnAppearing", OnAppearingAsync());
    }

    private async Task OnAppearingAsync()
    {
        _viewModel.EnsureDefaultSearchMode();
        await _viewModel.LoadClientesAsync();
    }
}
