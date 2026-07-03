using TipMolde.Diagnostics;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class FilaTrabalhoPage : ContentPage
{
    private readonly FilaTrabalhoViewModel _viewModel;

    public FilaTrabalhoPage(FilaTrabalhoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TaskMonitor.Observe("TipMolde.View.FilaTrabalhoPage.OnAppearing", OnAppearingAsync());
    }

    private async Task OnAppearingAsync()
    {
        await _viewModel.LoadFilaAsync();
    }
}
