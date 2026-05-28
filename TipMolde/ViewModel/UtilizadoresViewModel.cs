using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

public partial class UtilizadoresViewModel : SearchableViewModel
{
    private readonly UtilizadoresService _utilizadoresService;
    private readonly IDialogService _dialogService;

    public UtilizadoresViewModel(
        UtilizadoresService utilizadoresService,
        IDialogService dialogService)
    {
        _utilizadoresService = utilizadoresService;
        _dialogService = dialogService;
    }

    public ObservableCollection<UtilizadorDto> Utilizadores { get; } = new();

    [RelayCommand]
    public async Task LoadUtilizadoresAsync()
    {
        await ReloadCurrentPageAsync();
    }

    protected override async Task LoadPageAsync()
    {
        ErrorMessage = string.Empty;

        await ExecutePagedLoadAsync(async () =>
        {
            try
            {
                PagedResult<UtilizadorDto>? result;

                if (string.IsNullOrWhiteSpace(SearchTerm))
                {
                    result = await _utilizadoresService.GetUtilizadoresAsync(Page, PageSize);
                }
                else
                {
                    result = await _utilizadoresService.SearchAsync(SearchTerm.Trim(), Page, PageSize);
                }

                if (result is null)
                {
                    ErrorMessage = "Nao foi possivel carregar os utilizadores.";
                    return;
                }

                Utilizadores.Clear();

                foreach (var utilizador in result.Items)
                    Utilizadores.Add(utilizador);

                UpdatePagination(result.TotalItems, result.TotalPages);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        });
    }

    [RelayCommand]
    private async Task AbrirAdicionarUtilizadorAsync()
    {
        await Shell.Current.GoToAsync("AdicionarUtilizadorPage");
    }

    [RelayCommand]
    private async Task EditarCargoAsync(UtilizadorDto? utilizador)
    {
        if (utilizador is null)
            return;

        var roleOptions = UtilizadorDefaults.AvailableRoles
            .Where(role => !string.Equals(role, utilizador.Role, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var novoRole = await _dialogService.ShowSelectionAsync(
            $"Novo cargo para {utilizador.Nome}",
            "Cancelar",
            roleOptions);

        if (string.IsNullOrWhiteSpace(novoRole))
            return;

        try
        {
            await _utilizadoresService.UpdateUtilizadorRoleAsync(utilizador.User_id, novoRole);
            utilizador.Role = novoRole;
            await LoadUtilizadoresAsync();

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                $"O cargo do utilizador {utilizador.Nome} foi atualizado para {novoRole} com sucesso.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "Erro",
                ex.Message);
        }
    }

    [RelayCommand]
    private async Task ReporPasswordAsync(UtilizadorDto? utilizador)
    {
        if (utilizador is null)
            return;

        var novaPassword = await _dialogService.PromptAsync(
            "Repor password",
            $"Introduz a nova password para {utilizador.Nome}.{Environment.NewLine}Tem de ter pelo menos 8 caracteres, maiuscula, minuscula, numero e simbolo.",
            accept: "Guardar",
            cancel: "Cancelar",
            initialValue: UtilizadorDefaults.DefaultPassword,
            placeholder: "Ex.: TipMolde2026!",
            maxLength: 255,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(novaPassword))
            return;

        novaPassword = novaPassword.Trim();

        var passwordValidationError = UtilizadorDefaults.ValidatePassword(novaPassword);

        if (passwordValidationError is not null)
        {
            await _dialogService.ShowErrorAsync(
                "Erro",
                passwordValidationError);
            return;
        }

        try
        {
            await _utilizadoresService.ResetUtilizadorPasswordAsync(utilizador.User_id, novaPassword);
            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                $"A password do utilizador {utilizador.Nome} foi reposta com sucesso.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "Erro",
                ex.Message);
        }
    }

    [RelayCommand]
    private async Task AbrirOpcoesAsync(UtilizadorDto? utilizador)
    {
        if (utilizador is null)
            return;

        var action = await _dialogService.ShowOptionsAsync(utilizador.Nome, "Eliminar utilizador");

        if (action != "Eliminar utilizador")
            return;

        var confirmar = await _dialogService.ConfirmDeleteAsync(utilizador.Nome);

        if (!confirmar)
            return;

        try
        {
            await _utilizadoresService.DeleteUtilizadorAsync(utilizador.User_id);

            if (Utilizadores.Count == 1 && Page > 1)
                Page--;

            await LoadUtilizadoresAsync();

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                "O utilizador foi eliminado com sucesso.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "Erro",
                ex.Message);
        }
    }
}