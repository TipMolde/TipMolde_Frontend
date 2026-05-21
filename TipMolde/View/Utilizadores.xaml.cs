using TipMolde.Models;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class Utilizadores : ContentPage
{
    private readonly UtilizadoresViewModel _viewModel;
    public Utilizadores(UtilizadoresViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = vm;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadUtilizadoresAsync();
    }

    private async void OnEditRoleClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not UtilizadorDto utilizador)
            return;

        await DisplayAlert(
            "Editar cargo",
            $"Aqui vais abrir mais tarde a edição de cargo do utilizador {utilizador.Nome}.",
            "OK");
    }

    private async void OnResetPasswordClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not UtilizadorDto utilizador)
            return;

        await DisplayAlert(
            "Repor password",
            $"Aqui vais ligar mais tarde a reposição de password do utilizador {utilizador.Nome}.",
            "OK");
    }
}