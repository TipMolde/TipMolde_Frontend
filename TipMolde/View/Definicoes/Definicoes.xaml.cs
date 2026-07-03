using TipMolde.Diagnostics;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class Definicoes : ContentPage
{
    public Definicoes(DefinicoesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TaskMonitor.Observe("TipMolde.View.Definicoes.OnAppearing", OnAppearingAsync());
    }

    private async Task OnAppearingAsync()
    {
        if (BindingContext is DefinicoesViewModel vm)
            await vm.EnsureLoadedAsync(forceRefresh: true);
    }
}
