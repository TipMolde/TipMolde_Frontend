using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TipMolde.ViewModel;

public partial class SidebarViewModel : ObservableObject
{
    public SidebarViewModel()
    {
    }

    [RelayCommand]
    private async Task OpenDashboardAsync()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }

    [RelayCommand]
    private async Task OpenUtilizadoresAsync()
    {
        await Shell.Current.GoToAsync("//Utilizadores");
    }

    [RelayCommand]
    private async Task OpenClientesAsync()
    {
        await Shell.Current.GoToAsync("//Clientes");
    }

    [RelayCommand]
    private async Task OpenEncomendasAsync()
    {
        await Shell.Current.GoToAsync("//Encomendas");
    }

    [RelayCommand]
    private async Task OpenProducaoAsync()
    {
        await Shell.Current.GoToAsync("//Producao");
    }

    [RelayCommand]
    private async Task OpenDefinicoesAsync()
    {
        await Shell.Current.GoToAsync("//Definicoes");
    }
}