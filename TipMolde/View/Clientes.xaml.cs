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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.EnsureDefaultSearchMode();
        await _viewModel.LoadClientesAsync();
    }
}
