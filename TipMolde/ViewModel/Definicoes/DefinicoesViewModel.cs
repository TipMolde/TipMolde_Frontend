using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using TipMolde.Services;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere a area de definicoes pessoais da aplicacao.
/// </summary>
public partial class DefinicoesViewModel : ObservableObject
{
    private readonly UtilizadoresService _utilizadoresService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly IDialogService _dialogService;
    private readonly ThemePreferenceService _themePreferenceService;
    private readonly TopBarViewModel _topBarViewModel;

    private bool _isLoaded;
    private int? _currentUserId;

    /// <summary>
    /// Construtor do view model de definicoes.
    /// </summary>
    public DefinicoesViewModel(
        UtilizadoresService utilizadoresService,
        SessaoPersistidaService sessaoPersistidaService,
        IDialogService dialogService,
        ThemePreferenceService themePreferenceService,
        TopBarViewModel topBarViewModel)
    {
        _utilizadoresService = utilizadoresService;
        _sessaoPersistidaService = sessaoPersistidaService;
        _dialogService = dialogService;
        _themePreferenceService = themePreferenceService;
        _topBarViewModel = topBarViewModel;

        ThemeOptions = _themePreferenceService.AvailableThemes;
        SelectedThemeOption = ThemePreferenceService.GetStoredTheme();
    }

    public IReadOnlyList<string> ThemeOptions { get; }

    [ObservableProperty]
    private string nome = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string roleDisplay = "Sem perfil";

    [ObservableProperty]
    private string currentPassword = string.Empty;

    [ObservableProperty]
    private string newPassword = string.Empty;

    [ObservableProperty]
    private string confirmNewPassword = string.Empty;

    [ObservableProperty]
    private string selectedThemeOption = ThemePreferenceService.SystemThemeOption;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isSavingProfile;

    [ObservableProperty]
    private bool isChangingPassword;

    [ObservableProperty]
    private bool isApplyingTheme;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string UserInitials => BuildUserInitials(Nome);

    public string UserIdDisplay =>
        _currentUserId.HasValue
            ? "Conta autenticada"
            : "Sessao indisponivel";

    public string SelectedThemeDescription => SelectedThemeOption switch
    {
        ThemePreferenceService.LightThemeOption => "Ativa um aspeto claro em toda a aplicacao.",
        ThemePreferenceService.DarkThemeOption => "Ativa um aspeto escuro para reduzir brilho e fadiga visual.",
        _ => "Segue automaticamente o tema definido no teu dispositivo."
    };

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnNomeChanged(string value)
    {
        OnPropertyChanged(nameof(UserInitials));
    }

    partial void OnSelectedThemeOptionChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedThemeDescription));
    }

    public async Task EnsureLoadedAsync(bool forceRefresh = false)
    {
        if (_isLoaded && !forceRefresh)
            return;

        _isLoaded = true;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Reload()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SaveProfile()
    {
        if (IsSavingProfile)
            return;

        if (_currentUserId is null)
        {
            await _dialogService.ShowErrorAsync("Erro", "Não foi possível identificar a conta autenticada.");
            return;
        }

        var nomeNormalizado = Nome.Trim();
        var emailNormalizado = Email.Trim();

        var nomeError = UtilizadorDefaults.ValidateNome(nomeNormalizado);
        if (nomeError is not null)
        {
            await _dialogService.ShowErrorAsync("Erro", nomeError);
            return;
        }

        var emailError = UtilizadorDefaults.ValidateEmail(emailNormalizado);
        if (emailError is not null)
        {
            await _dialogService.ShowErrorAsync("Erro", emailError);
            return;
        }

        if (!new EmailAddressAttribute().IsValid(emailNormalizado))
        {
            await _dialogService.ShowErrorAsync("Erro", "O email introduzido não é válido.");
            return;
        }

        IsSavingProfile = true;

        try
        {
            await _utilizadoresService.UpdateAsync(_currentUserId.Value, nomeNormalizado, emailNormalizado);

            Nome = nomeNormalizado;
            Email = emailNormalizado;
            _topBarViewModel.CurrentUserName = nomeNormalizado;

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                "Os dados da conta foram atualizados com sucesso.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Erro", ex.Message);
        }
        finally
        {
            IsSavingProfile = false;
        }
    }

    [RelayCommand]
    private async Task ChangePassword()
    {
        if (IsChangingPassword)
            return;

        var currentPasswordNormalizada = CurrentPassword.Trim();
        var novaPasswordNormalizada = NewPassword.Trim();
        var confirmacaoPasswordNormalizada = ConfirmNewPassword.Trim();

        if (string.IsNullOrWhiteSpace(currentPasswordNormalizada))
        {
            await _dialogService.ShowErrorAsync("Erro", "Introduz a password atual.");
            return;
        }

        if (string.IsNullOrWhiteSpace(novaPasswordNormalizada))
        {
            await _dialogService.ShowErrorAsync("Erro", "Introduz a nova password.");
            return;
        }

        if (!string.Equals(novaPasswordNormalizada, confirmacaoPasswordNormalizada, StringComparison.Ordinal))
        {
            await _dialogService.ShowErrorAsync("Erro", "A confirmação da nova password não corresponde.");
            return;
        }

        if (string.Equals(currentPasswordNormalizada, novaPasswordNormalizada, StringComparison.Ordinal))
        {
            await _dialogService.ShowErrorAsync("Erro", "A nova password tem de ser diferente da atual.");
            return;
        }

        var passwordValidationError = UtilizadorDefaults.ValidatePassword(novaPasswordNormalizada);
        if (passwordValidationError is not null)
        {
            await _dialogService.ShowErrorAsync("Erro", passwordValidationError);
            return;
        }

        IsChangingPassword = true;

        try
        {
            await _utilizadoresService.ChangeCurrentPasswordAsync(currentPasswordNormalizada, novaPasswordNormalizada);

            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmNewPassword = string.Empty;

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                "A password foi alterada com sucesso.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Erro", ex.Message);
        }
        finally
        {
            IsChangingPassword = false;
        }
    }

    [RelayCommand]
    private Task ApplyTheme()
    {
        if (IsApplyingTheme)
            return Task.CompletedTask;

        IsApplyingTheme = true;

        try
        {
            ThemePreferenceService.ApplyTheme(SelectedThemeOption);
        }
        finally
        {
            IsApplyingTheme = false;
        }

        return Task.CompletedTask;
    }

    private async Task LoadAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        SelectedThemeOption = ThemePreferenceService.GetStoredTheme();

        try
        {
            _currentUserId = _sessaoPersistidaService.TryGetCurrentUserId();

            if (_currentUserId is null)
            {
                ErrorMessage = "Não foi possível identificar o utilizador autenticado.";
                return;
            }

            var utilizador = await _utilizadoresService.GetUtilizadorByIdAsync(_currentUserId.Value);

            Nome = utilizador.Nome;
            Email = utilizador.Email;
            RoleDisplay = FormatRole(utilizador.Role);

            OnPropertyChanged(nameof(UserIdDisplay));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string FormatRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return "Sem perfil";

        var normalized = role.Replace('_', ' ').ToLowerInvariant();
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(normalized);
    }

    private static string BuildUserInitials(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return "U";

        var parts = nome
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0]))
            .ToArray();

        return parts.Length == 0 ? "U" : new string(parts);
    }
}
