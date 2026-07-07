using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using TipMolde.Services;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere o formulario de criacao de utilizadores.
/// </summary>
public partial class AdicionarUtilizadorViewModel : ObservableObject
{
    private readonly UtilizadoresService _utilizadoresService;
    private readonly IDialogService _dialogService;

    /// <summary>
    /// Construtor do view model de criacao de utilizadores.
    /// </summary>
    /// <param name="utilizadoresService">Servico usado para persistir novos utilizadores.</param>
    /// <param name="dialogService">Servico usado para apresentar feedback ao utilizador.</param>
    public AdicionarUtilizadorViewModel(
        UtilizadoresService utilizadoresService,
        IDialogService dialogService)
    {
        _utilizadoresService = utilizadoresService;
        _dialogService = dialogService;
        SelectedRole = UtilizadorDefaults.AvailableRoles[0];
    }

    public IReadOnlyList<string> AvailableRoles => UtilizadorDefaults.AvailableRoles;

    [ObservableProperty]
    private string nome = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string selectedRole = string.Empty;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    [RelayCommand]
    private static async Task Voltar()
    {
        await ShellNavigationService.GoBackAsync();
    }

    /// <summary>
    /// Valida o formulario e cria um novo utilizador.
    /// </summary>
    /// <returns>Tarefa assincrona da operacao de criacao.</returns>
    [RelayCommand]
    private async Task Create()
    {
        if (IsSaving)
            return;

        ErrorMessage = string.Empty;

        var nomeNormalizado = Nome.Trim();
        var emailNormalizado = Email.Trim();
        var passwordNormalizada = Password.Trim();

        var nomeError = UtilizadorDefaults.ValidateNome(nomeNormalizado);
        if (nomeError is not null)
        {
            ErrorMessage = nomeError;
            return;
        }

        var emailError = UtilizadorDefaults.ValidateEmail(emailNormalizado);
        if (emailError is not null)
        {
            ErrorMessage = emailError;
            return;
        }

        if (!new EmailAddressAttribute().IsValid(emailNormalizado))
        {
            ErrorMessage = "O email introduzido nao e valido.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedRole))
        {
            ErrorMessage = "Tens de selecionar um cargo.";
            return;
        }

        var passwordValidationError = UtilizadorDefaults.ValidatePassword(passwordNormalizada);
        if (passwordValidationError is not null)
        {
            ErrorMessage = passwordValidationError;
            return;
        }

        IsSaving = true;

        try
        {
            await _utilizadoresService.CreateAsync(nomeNormalizado, emailNormalizado, passwordNormalizada, SelectedRole);

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                $"O utilizador {nomeNormalizado} foi criado com sucesso.");

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }
}

