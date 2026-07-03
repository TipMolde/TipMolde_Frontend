using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using TipMolde.Domain.Enums;
using TipMolde.Helper;
using TipMolde.Services;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere o formulario de criacao de moldes apenas com atributos do proprio molde.
/// </summary>
public partial class AdicionarMoldeViewModel : ObservableObject
{
    private readonly MoldesService _moldesService;
    private readonly IDialogService _dialogService;
    private readonly IFilePickerService _filePickerService;
    private FileResult? _imagemCapaSelecionada;
    private bool _loaded;

    /// <summary>
    /// Construtor do view model de criacao de moldes.
    /// </summary>
    public AdicionarMoldeViewModel(
        MoldesService moldesService,
        IDialogService dialogService,
        IFilePickerService filePickerService)
    {
        _moldesService = moldesService;
        _dialogService = dialogService;
        _filePickerService = filePickerService;
    }

    public ObservableCollection<TipoPedidoOption> TipoPedidoOptions { get; } = new();
    public ObservableCollection<CorMoldeOption> CorOptions { get; } = new();

    [ObservableProperty]
    private string numero = string.Empty;

    [ObservableProperty]
    private string numeroMoldeCliente = string.Empty;

    [ObservableProperty]
    private string nome = string.Empty;

    [ObservableProperty]
    private string imagemCapaPath = string.Empty;

    [ObservableProperty]
    private string descricao = string.Empty;

    [ObservableProperty]
    private int numeroCavidades = 1;

    [ObservableProperty]
    private string largura = string.Empty;

    [ObservableProperty]
    private string comprimento = string.Empty;

    [ObservableProperty]
    private string altura = string.Empty;

    [ObservableProperty]
    private string pesoEstimado = string.Empty;

    [ObservableProperty]
    private string tipoInjecao = string.Empty;

    [ObservableProperty]
    private string sistemaInjecao = string.Empty;

    [ObservableProperty]
    private string contracao = string.Empty;

    [ObservableProperty]
    private string acabamentoPeca = string.Empty;

    [ObservableProperty]
    private string materialMacho = string.Empty;

    [ObservableProperty]
    private string materialCavidade = string.Empty;

    [ObservableProperty]
    private string materialMovimentos = string.Empty;

    [ObservableProperty]
    private string materialInjecao = string.Empty;

    [ObservableProperty]
    private TipoPedidoOption? selectedTipoPedidoOption;

    [ObservableProperty]
    private CorMoldeOption? selectedCorOption;

    [ObservableProperty]
    private bool isLoadingData;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasImagemCapaSelecionada => !string.IsNullOrWhiteSpace(ImagemCapaPath);
    public string ImagemCapaPreviewSource => MoldeImageSourceHelper.Resolve(ImagemCapaPath);
    public string ImagemCapaFileName => string.IsNullOrWhiteSpace(ImagemCapaPath)
        ? "Imagem default da TipMolde"
        : Path.GetFileName(ImagemCapaPath.Trim());
    public bool CanCreate => !IsSaving
                             && !IsLoadingData
                             && !string.IsNullOrWhiteSpace(Numero)
                             && !string.IsNullOrWhiteSpace(Nome)
                             && SelectedTipoPedidoOption is not null;

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    partial void OnNumeroChanged(string value) => NotifyCanCreateStateChanged();

    partial void OnNomeChanged(string value) => NotifyCanCreateStateChanged();

    partial void OnImagemCapaPathChanged(string value)
    {
        OnPropertyChanged(nameof(HasImagemCapaSelecionada));
        OnPropertyChanged(nameof(ImagemCapaPreviewSource));
        OnPropertyChanged(nameof(ImagemCapaFileName));
    }

    partial void OnSelectedTipoPedidoOptionChanged(TipoPedidoOption? value) => NotifyCanCreateStateChanged();

    partial void OnSelectedCorOptionChanged(CorMoldeOption? value) => NotifyCanCreateStateChanged();

    partial void OnIsSavingChanged(bool value) => NotifyCanCreateStateChanged();

    partial void OnIsLoadingDataChanged(bool value) => NotifyCanCreateStateChanged();

    /// <summary>
    /// Carrega as opcoes de tipo de pedido disponiveis.
    /// </summary>
    public Task LoadAsync()
    {
        if (_loaded || IsLoadingData)
            return Task.CompletedTask;

        IsLoadingData = true;
        ErrorMessage = string.Empty;

        try
        {
            TipoPedidoOptions.Clear();
            foreach (var option in GetTipoPedidoOptions())
                TipoPedidoOptions.Add(option);

            CorOptions.Clear();
            foreach (var option in GetCorOptions())
                CorOptions.Add(option);

            if (SelectedTipoPedidoOption is null)
                SelectedTipoPedidoOption = TipoPedidoOptions.First();

            if (SelectedCorOption is null)
                SelectedCorOption = CorOptions.First();

            _loaded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoadingData = false;
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private static async Task Voltar()
    {
        await ShellNavigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task EscolherImagemCapa()
    {
        try
        {
            var file = await _filePickerService.PickAsync(new PickOptions
            {
                PickerTitle = "Selecionar imagem de capa",
                FileTypes = FilePickerFileType.Images
            });

            if (file is null || string.IsNullOrWhiteSpace(file.FullPath))
                return;

            _imagemCapaSelecionada = file;
            ImagemCapaPath = file.FullPath;
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nao foi possivel selecionar a imagem de capa. {ex.Message}";
        }
    }

    [RelayCommand]
    private void RemoverImagemCapa()
    {
        _imagemCapaSelecionada = null;
        ImagemCapaPath = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task Create()
    {
        if (IsSaving)
            return;

        var validationMessage = BuildValidationMessage();
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            ErrorMessage = validationMessage;
            return;
        }

        IsSaving = true;
        ErrorMessage = string.Empty;

        var numeroNormalizado = Numero.Trim();

        try
        {
            await _moldesService.CreateAsync(
                numeroNormalizado,
                NormalizeOptional(NumeroMoldeCliente),
                Nome.Trim(),
                NormalizeOptional(Descricao),
                NumeroCavidades,
                SelectedTipoPedidoOption!.Value,
                ParseOptionalDecimal(Largura),
                ParseOptionalDecimal(Comprimento),
                ParseOptionalDecimal(Altura),
                ParseOptionalDecimal(PesoEstimado),
                NormalizeOptional(TipoInjecao),
                NormalizeOptional(SistemaInjecao),
                ParseOptionalDecimal(Contracao),
                NormalizeOptional(AcabamentoPeca),
                SelectedCorOption?.Value,
                NormalizeOptional(MaterialMacho),
                NormalizeOptional(MaterialCavidade),
                NormalizeOptional(MaterialMovimentos),
                NormalizeOptional(MaterialInjecao),
                ImagemCapaPath);

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                $"O molde {numeroNormalizado} foi criado com sucesso.");

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
        if (string.IsNullOrWhiteSpace(Numero))
            return "Indique o numero do molde.";

        if (NumeroCavidades <= 0)
            return "Indique um numero valido de cavidades.";

        if (string.IsNullOrWhiteSpace(Nome))
            return "Indique o nome do molde.";

        if (SelectedTipoPedidoOption is null)
            return "Selecione o tipo de pedido.";

        if (!IsValidOptionalDecimal(Largura))
            return "A largura nao e valida.";

        if (!IsValidOptionalDecimal(Comprimento))
            return "O comprimento nao e valido.";

        if (!IsValidOptionalDecimal(Altura))
            return "A altura nao e valida.";

        if (!IsValidOptionalDecimal(PesoEstimado))
            return "O peso estimado nao e valido.";

        if (!IsValidOptionalDecimal(Contracao))
            return "A contracao nao e valida.";

        return string.Empty;
    }

    private static IReadOnlyList<TipoPedidoOption> GetTipoPedidoOptions()
    {
        return
        [
            new TipoPedidoOption("NOVO_MOLDE", "Novo Molde"),
            new TipoPedidoOption("REPARACAO", "Reparacao"),
            new TipoPedidoOption("ALTERACAO", "Alteracao")
        ];
    }

    private static IReadOnlyList<CorMoldeOption> GetCorOptions()
    {
        return
        [
            new CorMoldeOption(CorMolde.MONOCOLOR, "Monocolor"),
            new CorMoldeOption(CorMolde.BICOLOR, "Bicolor"),
            new CorMoldeOption(CorMolde.OUTRO, "Outro")
        ];
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static decimal? ParseOptionalDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return decimal.Parse(value.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture);
    }

    private static bool IsValidOptionalDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        return decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out _);
    }

    private void NotifyCanCreateStateChanged()
    {
        OnPropertyChanged(nameof(CanCreate));
        CreateCommand.NotifyCanExecuteChanged();
    }
}

/// <summary>
/// Opcao de tipo de pedido apresentada no picker.
/// </summary>
public sealed record TipoPedidoOption(string Value, string DisplayName);

/// <summary>
/// Opcao de cor apresentada no picker.
/// </summary>
public sealed record CorMoldeOption(CorMolde Value, string DisplayName);
