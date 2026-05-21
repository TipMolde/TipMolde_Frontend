using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class Producao : ContentPage
{
    public Producao(ProducaoViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}