using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere a listagem e as operacoes administrativas sobre utilizadores.
/// </summary>
public partial class UtilizadoresViewModel : SearchableViewModel
{
    private readonly UtilizadoresService _utilizadoresService;
    private readonly IDialogService _dialogService;

    /// <summary>
    /// Construtor do view model de utilizadores.
    /// </summary>
    /// <param name="utilizadoresService">Servico usado para consultar e alterar utilizadores.</param>
    /// <param name="dialogService">Servico usado para dialogs e feedback da UI.</param>
    public UtilizadoresViewModel(
        UtilizadoresService utilizadoresService,
        IDialogService dialogService)
    {
        _utilizadoresService = utilizadoresService;
        _dialogService = dialogService;
    }

    public ObservableCollection<UtilizadorDto> Utilizadores { get; } = new();

    /// <summary>
    /// Carrega a lista paginada de utilizadores.
    /// </summary>
    /// <returns>Tarefa assincrona do carregamento da lista.</returns>
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

    /// <summary>
    /// Abre o formulario de criacao de utilizador.
    /// </summary>
    /// <returns>Tarefa assincrona da navegacao para criacao.</returns>
    [RelayCommand]
    private static async Task AbrirAdicionarUtilizadorAsync()
    {
        await Shell.Current.GoToAsync("AdicionarUtilizadorPage");
    }

    /// <summary>
    /// Permite alterar a role do utilizador selecionado.
    /// </summary>
    /// <param name="utilizador">Utilizador alvo da alteracao.</param>
    /// <returns>Tarefa assincrona da operacao de alteracao de cargo.</returns>
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

    /// <summary>
    /// Permite repor a password do utilizador selecionado.
    /// </summary>
    /// <param name="utilizador">Utilizador alvo da reposicao.</param>
    /// <returns>Tarefa assincrona da operacao de reposicao.</returns>
    [RelayCommand]
    private async Task ReporPasswordAsync(UtilizadorDto? utilizador)
    {
        if (utilizador is null)
            return;

        var novaPassword = await _dialogService.PromptAsync(
            "Repor password",
            $"Introduz a nova password para {utilizador.Nome}.{Environment.NewLine}Podes usar a password temporaria acordada pela equipa, ou definir outra.",
            new PromptDialogOptions
            {
                Accept = "Guardar",
                Cancel = "Cancelar",
                Placeholder = UtilizadorDefaults.PasswordSuggestionHint,
                MaxLength = 255,
                Keyboard = Keyboard.Text
            });

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

    /// <summary>
    /// Abre o menu de opcoes administrativas para um utilizador.
    /// </summary>
    /// <param name="utilizador">Utilizador alvo das opcoes.</param>
    /// <returns>Tarefa assincrona da operacao administrativa.</returns>
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
