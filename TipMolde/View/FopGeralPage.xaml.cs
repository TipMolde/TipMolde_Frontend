using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class FopGeralPage : ContentPage
{
    private readonly FopGeralViewModel _viewModel;

    public FopGeralPage(FopGeralViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
