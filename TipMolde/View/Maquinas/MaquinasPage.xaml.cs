using TipMolde.Diagnostics;
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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TaskMonitor.Observe("TipMolde.View.MaquinasPage.OnAppearing", OnAppearingAsync());
    }

    private async Task OnAppearingAsync()
    {
        await _viewModel.LoadAsync();
    }
}
