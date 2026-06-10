using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class EditarMaquinaViewModel : ObservableObject
{
    private readonly MaquinasService _maquinasService;
    private readonly IDialogService _dialogService;

    public EditarMaquinaViewModel(
        MaquinasService maquinasService,
        IDialogService dialogService)
    {
        _maquinasService = maquinasService;
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

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string NumeroDisplay => Numero <= 0 ? "Sem numero" : Numero.ToString();
    public string NomeModeloDisplay => string.IsNullOrWhiteSpace(NomeModelo) ? "Maquina sem nome" : NomeModelo.Trim();
    public string FaseDedicadaDisplay => string.IsNullOrWhiteSpace(FaseDedicada) ? "Sem fase dedicada" : FaseDedicada;
    public string EstadoAtualDisplay => string.IsNullOrWhiteSpace(EstadoAtualOriginal) ? "Sem estado" : EstadoAtualOriginal.Replace('_', ' ');
    public string MaquinaDisplay => $"{NumeroDisplay} - {NomeModeloDisplay}";
    public string TransicoesPermitidasDisplay => BuildTransicoesPermitidasDisplay();
    public bool CanSave => MaquinaId > 0
                           && !IsLoading
                           && !IsSaving
                           && Numero > 0
                           && !string.IsNullOrWhiteSpace(NomeModelo)
                           && SelectedEstadoMaquinaOption is not null
                           && HasChanges();

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

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

    public Task LoadAsync(
        int maquinaId,
        int numero,
        string? nomeModelo,
        string? faseDedicada,
        string? estadoAtual,
        string? ipAddress)
    {
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

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task VoltarAsync()
    {
        await Shell.Current.GoToAsync("..");
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
            var numeroAlterado = Numero != NumeroOriginal;
            var nomeAlterado = !string.Equals(nomeNormalizado, NomeModeloOriginal?.Trim(), StringComparison.Ordinal);
            var ipAlterado = !string.Equals(
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
                $"A maquina {NumeroDisplay} foi atualizada com sucesso.");

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

        if (Numero <= 0)
            return "Indique um numero valido para a maquina.";

        if (string.IsNullOrWhiteSpace(NomeModelo))
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

        return Numero != NumeroOriginal
            || !string.Equals(normalizedCurrentNome, normalizedNewNome, StringComparison.Ordinal)
            || !string.Equals(normalizedCurrentIp, normalizedNewIp, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(normalizedCurrentEstado, normalizedNewEstado, StringComparison.OrdinalIgnoreCase);
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
