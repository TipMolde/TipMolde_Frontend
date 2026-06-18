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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(forceRefresh: true);
    }
}
