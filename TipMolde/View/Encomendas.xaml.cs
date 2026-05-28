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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadEncomendasAsync();
    }
}
