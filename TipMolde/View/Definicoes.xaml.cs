using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class Definicoes : ContentPage
{
	public Definicoes(DefinicoesViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}