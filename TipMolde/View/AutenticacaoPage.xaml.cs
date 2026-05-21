using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class AutenticacaoPage : ContentPage
{
    public AutenticacaoPage(AutenticacaoViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is AutenticacaoViewModel vm)
        {
            await vm.EnsureInitialLoadCommand.ExecuteAsync(null);
        }
    }
}
