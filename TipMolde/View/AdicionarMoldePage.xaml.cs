using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class AdicionarMoldePage : ContentPage
{
    private readonly AdicionarMoldeViewModel _viewModel;

    public AdicionarMoldePage(AdicionarMoldeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
