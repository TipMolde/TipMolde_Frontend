using TipMolde.Diagnostics;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class ProjetosPage : ContentPage
{
    private readonly ProjetosViewModel _viewModel;

    public ProjetosPage(ProjetosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TaskMonitor.Observe("TipMolde.View.ProjetosPage.OnAppearing", OnAppearingAsync());
    }

    private async Task OnAppearingAsync()
    {
        await _viewModel.LoadAsync();
    }
}
