using TipMolde.Diagnostics;
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
    protected override void OnAppearing()
    {
        base.OnAppearing();
        TaskMonitor.Observe("TipMolde.View.Utilizadores.OnAppearing", OnAppearingAsync());
    }

    private async Task OnAppearingAsync()
    {
        await _viewModel.LoadUtilizadoresAsync();
    }
}
