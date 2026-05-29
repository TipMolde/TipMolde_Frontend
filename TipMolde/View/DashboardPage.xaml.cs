using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class MainPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public MainPage(DashboardViewModel vm)
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
