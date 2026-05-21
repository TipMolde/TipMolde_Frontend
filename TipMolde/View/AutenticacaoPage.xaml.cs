using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class AutenticacaoPage : ContentPage
{
    public AutenticacaoPage(AutenticacaoViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}