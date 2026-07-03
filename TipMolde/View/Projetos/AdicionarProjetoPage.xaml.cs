using TipMolde.Diagnostics;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class AdicionarProjetoPage : ContentPage
{
    private readonly AdicionarProjetoViewModel _viewModel;

    public AdicionarProjetoPage(AdicionarProjetoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TaskMonitor.Observe("TipMolde.View.AdicionarProjetoPage.OnAppearing", OnAppearingAsync());
    }

    private async Task OnAppearingAsync()
    {
        await _viewModel.LoadAsync();
    }
}
