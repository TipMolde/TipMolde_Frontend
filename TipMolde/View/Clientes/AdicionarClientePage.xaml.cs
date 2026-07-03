using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class AdicionarClientePage : ContentPage
{
    public AdicionarClientePage(AdicionarClienteViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
