using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class AdicionarUtilizadorPage : ContentPage
{
    public AdicionarUtilizadorPage(AdicionarUtilizadorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
