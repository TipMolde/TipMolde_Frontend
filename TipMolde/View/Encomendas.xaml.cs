using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class Encomendas : ContentPage
{
	public Encomendas(EncomendasViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}