using Microsoft.Extensions.DependencyInjection;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.EnsureInitialLoadCommand.ExecuteAsync(null);
    }
}
