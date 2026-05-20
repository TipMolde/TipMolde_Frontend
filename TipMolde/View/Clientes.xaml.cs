using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class Clientes : ContentPage
{
	public Clientes(ClientesViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}