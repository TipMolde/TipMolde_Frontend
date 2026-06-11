using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using System.IO;
using TipMolde.Helper;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere o formulario de criacao de moldes apenas com atributos do proprio molde.
/// </summary>
public partial class AdicionarMoldeViewModel : ObservableObject
{
    private readonly MoldesService _moldesService;
    private readonly IDialogService _dialogService;
    private bool _loaded;

    /// <summary>
    /// Construtor do view model de criacao de moldes.
    /// </summary>
    public AdicionarMoldeViewModel(
        MoldesService moldesService,
        IDialogService dialogService)
    {
        _moldesService = moldesService;
        _dialogService = dialogService;
    }

    public ObservableCollection<TipoPedidoOption> TipoPedidoOptions { get; } = new();

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
    private TipoPedidoOption? selectedTipoPedidoOption;

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
        ? "Sem imagem selecionada."
        : Path.GetFileName(ImagemCapaPath.Trim());
    public bool CanCreate => !IsSaving
                             && !IsLoadingData
                             && !string.IsNullOrWhiteSpace(Numero)
                             && !string.IsNullOrWhiteSpace(Nome)
                             && SelectedTipoPedidoOption is not null;

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    partial void OnNumeroChanged(string value)
    {
        OnPropertyChanged(nameof(CanCreate));
        CreateCommand.NotifyCanExecuteChanged();
    }

    partial void OnNomeChanged(string value)
    {
        OnPropertyChanged(nameof(CanCreate));
        CreateCommand.NotifyCanExecuteChanged();
    }

    partial void OnImagemCapaPathChanged(string value)
    {
        OnPropertyChanged(nameof(HasImagemCapaSelecionada));
        OnPropertyChanged(nameof(ImagemCapaPreviewSource));
        OnPropertyChanged(nameof(ImagemCapaFileName));
    }

    partial void OnSelectedTipoPedidoOptionChanged(TipoPedidoOption? value)
    {
        OnPropertyChanged(nameof(CanCreate));
        CreateCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSavingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCreate));
        CreateCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingDataChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCreate));
        CreateCommand.NotifyCanExecuteChanged();
    }

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

            if (SelectedTipoPedidoOption is null)
                SelectedTipoPedidoOption = TipoPedidoOptions.FirstOrDefault();

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
    private async Task Voltar()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task EscolherImagemCapa()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
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
                NormalizeOptional(ImagemCapaPath),
                NormalizeOptional(Descricao),
                NumeroCavidades,
                SelectedTipoPedidoOption!.Value);

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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

/// <summary>
/// Opcao de tipo de pedido apresentada no picker.
/// </summary>
public sealed record TipoPedidoOption(string Value, string DisplayName);
