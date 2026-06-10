using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class DesenhoPage : ContentPage
{
    private readonly DesenhoViewModel _viewModel;

    public DesenhoPage(DesenhoViewModel viewModel)
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
