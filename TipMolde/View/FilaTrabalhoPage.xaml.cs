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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadFilaAsync();
    }
}
