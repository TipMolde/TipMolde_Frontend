using TipMolde.Diagnostics;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class AutenticacaoPage : ContentPage
{
    public AutenticacaoPage(AutenticacaoViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TaskMonitor.Observe("TipMolde.View.AutenticacaoPage.OnAppearing", OnAppearingAsync());
    }

    private async Task OnAppearingAsync()
    {
        if (BindingContext is AutenticacaoViewModel vm)
            await vm.EnsureInitialLoadCommand.ExecuteAsync(null);
    }
}
