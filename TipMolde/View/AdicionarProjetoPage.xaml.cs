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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
