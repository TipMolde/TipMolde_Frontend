using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TipMolde.ViewModel;

public partial class AutenticacaoViewModel : ObservableObject
{
	public AutenticacaoViewModel()
	{
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}