using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class AdicionarEncomendaPage : ContentPage
{
    private readonly AdicionarEncomendaViewModel _viewModel;

    public AdicionarEncomendaPage(AdicionarEncomendaViewModel viewModel)
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
