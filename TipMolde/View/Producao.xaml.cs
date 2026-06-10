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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
