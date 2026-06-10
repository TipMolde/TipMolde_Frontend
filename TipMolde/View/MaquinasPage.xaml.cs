using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class MaquinasPage : ContentPage
{
    private readonly MaquinasViewModel _viewModel;

    public MaquinasPage(MaquinasViewModel viewModel)
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
