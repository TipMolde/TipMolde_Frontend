using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using TipMolde.Domain.Enums;
using TipMolde.Helper;
using TipMolde.Services;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere o formulario de edicao de moldes com os atributos do proprio molde.
/// </summary>
public partial class EditarMoldeViewModel : ObservableObject
{
    private readonly MoldesService _moldesService;
    private readonly IDialogService _dialogService;
    private readonly AuthorizationService _authorizationService;
    private readonly IFilePickerService _filePickerService;
    private bool _loaded;

    public EditarMoldeViewModel(
        MoldesService moldesService,
        IDialogService dialogService,
        AuthorizationService authorizationService,
        IFilePickerService filePickerService)
    {
        _moldesService = moldesService;
        _dialogService = dialogService;
        _authorizationService = authorizationService;
        _filePickerService = filePickerService;
    }

    public ObservableCollection<TipoPedidoOption> TipoPedidoOptions { get; } = new();
    public ObservableCollection<CorMoldeOption> CorOptions { get; } = new();

    [ObservableProperty]
    private int moldeId;

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
    public bool CanSave => !IsSaving
                           && !IsLoadingData
                           && MoldeId > 0
                           && !string.IsNullOrWhiteSpace(Numero)
                           && !string.IsNullOrWhiteSpace(Nome)
                           && SelectedTipoPedidoOption is not null;

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnNumeroChanged(string value) => NotifyCanSaveStateChanged();
    partial void OnNomeChanged(string value) => NotifyCanSaveStateChanged();
    partial void OnImagemCapaPathChanged(string value)
    {
        OnPropertyChanged(nameof(HasImagemCapaSelecionada));
        OnPropertyChanged(nameof(ImagemCapaPreviewSource));
        OnPropertyChanged(nameof(ImagemCapaFileName));
    }
    partial void OnSelectedTipoPedidoOptionChanged(TipoPedidoOption? value) => NotifyCanSaveStateChanged();
    partial void OnIsSavingChanged(bool value) => NotifyCanSaveStateChanged();
    partial void OnIsLoadingDataChanged(bool value) => NotifyCanSaveStateChanged();

    public async Task LoadAsync(int moldeId)
    {
        MoldeId = moldeId;
        IsLoadingData = true;
        ErrorMessage = string.Empty;

        try
        {
            if (!_authorizationService.CanCreateMachines())
            {
                ErrorMessage = "Apenas o administrador pode editar moldes.";
                return;
            }

            if (!_loaded)
            {
                TipoPedidoOptions.Clear();
                foreach (var option in GetTipoPedidoOptions())
                    TipoPedidoOptions.Add(option);

                CorOptions.Clear();
                foreach (var option in GetCorOptions())
                    CorOptions.Add(option);
            }

            var molde = await _moldesService.GetByIdAsync(moldeId);
            if (molde is null)
            {
                ErrorMessage = "Nao foi possivel carregar o molde para edicao.";
                return;
            }

            Numero = molde.Numero ?? string.Empty;
            NumeroMoldeCliente = molde.NumeroMoldeCliente ?? string.Empty;
            Nome = molde.Nome ?? string.Empty;
            ImagemCapaPath = molde.ImagemCapaPath ?? string.Empty;
            Descricao = molde.Descricao ?? string.Empty;
            NumeroCavidades = molde.Numero_cavidades;
            Largura = FormatOptionalDecimal(molde.Largura);
            Comprimento = FormatOptionalDecimal(molde.Comprimento);
            Altura = FormatOptionalDecimal(molde.Altura);
            PesoEstimado = FormatOptionalDecimal(molde.PesoEstimado);
            TipoInjecao = molde.TipoInjecao ?? string.Empty;
            SistemaInjecao = molde.SistemaInjecao ?? string.Empty;
            Contracao = FormatOptionalDecimal(molde.Contracao);
            AcabamentoPeca = molde.AcabamentoPeca ?? string.Empty;
            MaterialMacho = molde.MaterialMacho ?? string.Empty;
            MaterialCavidade = molde.MaterialCavidade ?? string.Empty;
            MaterialMovimentos = molde.MaterialMovimentos ?? string.Empty;
            MaterialInjecao = molde.MaterialInjecao ?? string.Empty;

            SelectedTipoPedidoOption = TipoPedidoOptions.FirstOrDefault(option => option.Value == molde.TipoPedido)
                ?? TipoPedidoOptions[0];
            SelectedCorOption = molde.Cor is null
                ? null
                : CorOptions.FirstOrDefault(option => option.Value == molde.Cor.Value);

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
    }

    [RelayCommand]
    private static async Task Voltar()
    {
        await Shell.Current.GoToAsync("..");
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
        ImagemCapaPath = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task GuardarAsync()
    {
        if (IsSaving)
            return;

        if (!_authorizationService.CanCreateMachines())
        {
            ErrorMessage = "Apenas o administrador pode editar moldes.";
            return;
        }

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
            await _moldesService.UpdateAsync(
                MoldeId,
                NormalizeOptional(Numero),
                NormalizeOptional(NumeroMoldeCliente),
                NormalizeOptional(Nome),
                null,
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
                NormalizeOptional(MaterialInjecao));

            if (File.Exists(ImagemCapaPath))
            {
                await _moldesService.UpdateImagemCapaAsync(MoldeId, ImagemCapaPath);
            }

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                $"O molde {Numero.Trim()} foi atualizado com sucesso.");

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

    private static string FormatOptionalDecimal(decimal? value)
    {
        return value.HasValue
            ? value.Value.ToString(CultureInfo.CurrentCulture)
            : string.Empty;
    }

    private static bool IsValidOptionalDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        return decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out _);
    }

    private void NotifyCanSaveStateChanged()
    {
        OnPropertyChanged(nameof(CanSave));
        GuardarCommand.NotifyCanExecuteChanged();
    }
}
