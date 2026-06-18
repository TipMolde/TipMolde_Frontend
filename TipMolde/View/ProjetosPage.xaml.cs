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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
