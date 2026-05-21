using TipMolde.Models;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class Utilizadores : ContentPage
{
    private readonly UtilizadoresViewModel _viewModel;
    public Utilizadores(UtilizadoresViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = vm;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadUtilizadoresAsync();
    }
}