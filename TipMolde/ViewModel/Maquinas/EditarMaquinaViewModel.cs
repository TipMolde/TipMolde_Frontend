using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Services;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere a edicao de uma maquina existente, incluindo permissoes e transicoes de estado.
/// </summary>
public partial class EditarMaquinaViewModel : ObservableObject
{
    private readonly MaquinasService _maquinasService;
    private readonly AuthorizationService _authorizationService;
    private readonly IDialogService _dialogService;
    private bool _permissionsLoaded;

    /// <summary>
    /// Construtor do view model de edicao de maquinas.
    /// </summary>
    /// <param name="maquinasService">Servico usado para persistir alteracoes da maquina.</param>
    /// <param name="authorizationService">Servico usado para validar permissoes do utilizador atual.</param>
    /// <param name="dialogService">Servico usado para apresentar mensagens de feedback.</param>
    public EditarMaquinaViewModel(
        MaquinasService maquinasService,
        AuthorizationService authorizationService,
        IDialogService dialogService)
    {
        _maquinasService = maquinasService;
        _authorizationService = authorizationService;
        _dialogService = dialogService;
    }

    public ObservableCollection<EstadoMaquinaOption> EstadoMaquinaOptions { get; } = new();

    [ObservableProperty]
    private int maquinaId;

    [ObservableProperty]
    private int numero;

    [ObservableProperty]
    private string nomeModelo = string.Empty;

    [ObservableProperty]
    private string faseDedicada = string.Empty;

    [ObservableProperty]
    private string estadoAtual = string.Empty;

    [ObservableProperty]
    private string ipAddress = string.Empty;

    [ObservableProperty]
    private EstadoMaquinaOption? selectedEstadoMaquinaOption;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private int numeroOriginal;

    [ObservableProperty]
    private string nomeModeloOriginal = string.Empty;

    [ObservableProperty]
    private string estadoAtualOriginal = string.Empty;

    [ObservableProperty]
    private string ipAddressOriginal = string.Empty;

    [ObservableProperty]
    private bool canEditMachineAdministrativeFields;

    [ObservableProperty]
    private bool canEditMachineState;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string NumeroDisplay => Numero <= 0 ? "Sem numero" : Numero.ToString();
    public string NomeModeloDisplay => string.IsNullOrWhiteSpace(NomeModelo) ? "Maquina sem nome" : NomeModelo.Trim();
    public string FaseDedicadaDisplay => string.IsNullOrWhiteSpace(FaseDedicada) ? "Sem fase dedicada" : FaseDedicada;
    public string EstadoAtualDisplay => string.IsNullOrWhiteSpace(EstadoAtualOriginal) ? "Sem estado" : EstadoAtualOriginal.Replace('_', ' ');
    public string MaquinaDisplay => NomeModeloDisplay;
    public string TransicoesPermitidasDisplay => BuildTransicoesPermitidasDisplay();
    public bool IsStateOnlyEditMode => CanEditMachineState && !CanEditMachineAdministrativeFields;
    public bool CanSave => MaquinaId > 0
                           && CanEditMachineState
                           && !IsLoading
                           && !IsSaving
                           && SelectedEstadoMaquinaOption is not null
                           && HasRequiredEditableFields()
                           && HasChanges();

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    partial void OnCanEditMachineAdministrativeFieldsChanged(bool value)
    {
        OnPropertyChanged(nameof(IsStateOnlyEditMode));
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnCanEditMachineStateChanged(bool value)
    {
        OnPropertyChanged(nameof(IsStateOnlyEditMode));
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnNumeroChanged(int value)
    {
        OnPropertyChanged(nameof(NumeroDisplay));
        OnPropertyChanged(nameof(MaquinaDisplay));
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnNomeModeloChanged(string value)
    {
        OnPropertyChanged(nameof(NomeModeloDisplay));
        OnPropertyChanged(nameof(MaquinaDisplay));
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnFaseDedicadaChanged(string value) => OnPropertyChanged(nameof(FaseDedicadaDisplay));

    partial void OnEstadoAtualOriginalChanged(string value)
    {
        OnPropertyChanged(nameof(EstadoAtualDisplay));
        OnPropertyChanged(nameof(TransicoesPermitidasDisplay));
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnIpAddressChanged(string value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedEstadoMaquinaOptionChanged(EstadoMaquinaOption? value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSavingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnNumeroOriginalChanged(int value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnNomeModeloOriginalChanged(string value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnIpAddressOriginalChanged(string value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Inicializa o formulario com os dados da maquina a editar.
    /// </summary>
    /// <param name="maquinaId">Identificador interno da maquina.</param>
    /// <param name="numero">Numero operacional apresentado ao utilizador.</param>
    /// <param name="nomeModelo">Nome ou modelo configurado para a maquina.</param>
    /// <param name="faseDedicada">Descricao da fase de producao dedicada.</param>
    /// <param name="estadoAtual">Estado atual registado para a maquina.</param>
    /// <param name="ipAddress">Endereco IP configurado para a ligacao industrial.</param>
    /// <returns>Tarefa assincrona da operacao de carregamento.</returns>
    public async Task LoadAsync(
        int maquinaId,
        int numero,
        string? nomeModelo,
        string? faseDedicada,
        string? estadoAtual,
        string? ipAddress)
    {
        await EnsurePermissionsLoadedAsync();

        MaquinaId = maquinaId;
        Numero = numero;
        NumeroOriginal = numero;
        NomeModelo = nomeModelo?.Trim() ?? string.Empty;
        NomeModeloOriginal = NomeModelo;
        FaseDedicada = faseDedicada ?? string.Empty;
        EstadoAtual = estadoAtual ?? string.Empty;
        EstadoAtualOriginal = EstadoAtual;
        IpAddress = ipAddress ?? string.Empty;
        IpAddressOriginal = IpAddress;
        ErrorMessage = string.Empty;

        EstadoMaquinaOptions.Clear();
        foreach (var option in GetEstadoOptionsParaEdicao(EstadoAtualOriginal))
            EstadoMaquinaOptions.Add(option);

        SelectedEstadoMaquinaOption = EstadoMaquinaOptions
            .FirstOrDefault(item => string.Equals(item.Value, EstadoAtualOriginal, StringComparison.OrdinalIgnoreCase));

        ErrorMessage = CanEditMachineState
            ? string.Empty
            : "Nao tens permissao para editar esta maquina.";
    }

    [RelayCommand]
    private static async Task VoltarAsync()
    {
        await ShellNavigationService.GoBackAsync();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (IsSaving || SelectedEstadoMaquinaOption is null)
            return;

        var validationMessage = BuildValidationMessage();
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            ErrorMessage = validationMessage;
            return;
        }

        IsSaving = true;
        ErrorMessage = string.Empty;

        try
        {
            var nomeNormalizado = NomeModelo.Trim();
            var ipNormalizado = NormalizeOptional(IpAddress);
            var numeroAlterado = CanEditMachineAdministrativeFields && Numero != NumeroOriginal;
            var nomeAlterado = CanEditMachineAdministrativeFields &&
                               !string.Equals(nomeNormalizado, NomeModeloOriginal?.Trim(), StringComparison.Ordinal);
            var ipAlterado = CanEditMachineAdministrativeFields &&
                             !string.Equals(
                                 ipNormalizado,
                                 NormalizeOptional(IpAddressOriginal),
                                 StringComparison.OrdinalIgnoreCase);
            var estadoAlterado = !string.Equals(
                NormalizeEstado(EstadoAtualOriginal),
                NormalizeEstado(SelectedEstadoMaquinaOption.Value),
                StringComparison.OrdinalIgnoreCase);

            await _maquinasService.UpdateAsync(
                MaquinaId,
                numero: numeroAlterado ? Numero : null,
                nomeModelo: nomeAlterado ? nomeNormalizado : null,
                ipAddress: ipAlterado ? ipNormalizado : null,
                estado: estadoAlterado ? SelectedEstadoMaquinaOption.Value : null);

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                $"A maquina {NomeModeloDisplay} foi atualizada com sucesso.");

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

    private string BuildValidationMessage()
    {
        if (MaquinaId <= 0)
            return "Nao foi possivel identificar a maquina a editar.";

        if (!CanEditMachineState)
            return "Nao tens permissao para editar esta maquina.";

        if (CanEditMachineAdministrativeFields && Numero <= 0)
            return "Indique um numero valido para a maquina.";

        if (CanEditMachineAdministrativeFields && string.IsNullOrWhiteSpace(NomeModelo))
            return "Indique o nome ou modelo da maquina.";

        if (SelectedEstadoMaquinaOption is null)
            return "Selecione um estado para a maquina.";

        return string.Empty;
    }

    private bool HasChanges()
    {
        if (SelectedEstadoMaquinaOption is null)
            return false;

        var normalizedCurrentIp = NormalizeOptional(IpAddressOriginal);
        var normalizedNewIp = NormalizeOptional(IpAddress);
        var normalizedCurrentEstado = NormalizeEstado(EstadoAtualOriginal);
        var normalizedNewEstado = NormalizeEstado(SelectedEstadoMaquinaOption.Value);
        var normalizedCurrentNome = (NomeModeloOriginal ?? string.Empty).Trim();
        var normalizedNewNome = (NomeModelo ?? string.Empty).Trim();

        var administrativeChanges = CanEditMachineAdministrativeFields &&
                                    (Numero != NumeroOriginal
                                     || !string.Equals(normalizedCurrentNome, normalizedNewNome, StringComparison.Ordinal)
                                     || !string.Equals(normalizedCurrentIp, normalizedNewIp, StringComparison.OrdinalIgnoreCase));

        var stateChange = !string.Equals(normalizedCurrentEstado, normalizedNewEstado, StringComparison.OrdinalIgnoreCase);

        return administrativeChanges || stateChange;
    }

    private bool HasRequiredEditableFields()
    {
        if (!CanEditMachineAdministrativeFields)
            return true;

        return Numero > 0 && !string.IsNullOrWhiteSpace(NomeModelo);
    }

    private async Task EnsurePermissionsLoadedAsync(bool forceRefresh = false)
    {
        if (_permissionsLoaded && !forceRefresh)
            return;

        await _authorizationService.GetCurrentRoleAsync(forceRefresh);
        CanEditMachineAdministrativeFields = _authorizationService.CanEditMachineAdministrativeFields();
        CanEditMachineState = _authorizationService.CanEditMachineState();
        _permissionsLoaded = true;
    }

    private string BuildTransicoesPermitidasDisplay()
    {
        return NormalizeEstado(EstadoAtualOriginal) switch
        {
            "DISPONIVEL" => "Transicoes permitidas: Disponivel -> Manutencao.",
            "EM_USO" => "Transicoes permitidas: Em Uso -> Manutencao.",
            "MANUTENCAO" => "Transicoes permitidas: Manutencao -> Disponivel.",
            _ => "Transicoes permitidas: manter estado atual."
        };
    }

    private static IReadOnlyList<EstadoMaquinaOption> GetEstadoOptionsParaEdicao(string? estadoAtual)
    {
        return NormalizeEstado(estadoAtual) switch
        {
            "DISPONIVEL" =>
            [
                new EstadoMaquinaOption("DISPONIVEL", "Disponivel"),
                new EstadoMaquinaOption("MANUTENCAO", "Manutencao")
            ],
            "EM_USO" =>
            [
                new EstadoMaquinaOption("EM_USO", "Em Uso"),
                new EstadoMaquinaOption("MANUTENCAO", "Manutencao")
            ],
            "MANUTENCAO" =>
            [
                new EstadoMaquinaOption("MANUTENCAO", "Manutencao"),
                new EstadoMaquinaOption("DISPONIVEL", "Disponivel")
            ],
            _ =>
            [
                new EstadoMaquinaOption(NormalizeEstado(estadoAtual), string.IsNullOrWhiteSpace(estadoAtual) ? "Sem estado" : estadoAtual.Replace('_', ' '))
            ]
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeEstado(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }
}
