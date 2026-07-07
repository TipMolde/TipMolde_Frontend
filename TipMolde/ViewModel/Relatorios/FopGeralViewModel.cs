using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere a consulta e exportacao da FOP geral.
/// </summary>
public partial class FopGeralViewModel : PaginatedViewModel
{
    private const int DefaultPageSize = 20;

    private readonly RelatoriosService _relatoriosService;
    private readonly IDestinationFolderPickerService _destinationFolderPickerService;
    private readonly IDialogService _dialogService;

    /// <summary>
    /// Construtor do view model da FOP geral.
    /// </summary>
    public FopGeralViewModel(
        RelatoriosService relatoriosService,
        IDestinationFolderPickerService destinationFolderPickerService,
        IDialogService dialogService)
    {
        _relatoriosService = relatoriosService;
        _destinationFolderPickerService = destinationFolderPickerService;
        _dialogService = dialogService;
        PageSize = DefaultPageSize;
        DataInicio = DateTime.Today.AddDays(-30);
        DataFim = DateTime.Today;
    }

    public ObservableCollection<FopGeralLinhaDto> Linhas { get; } = new();

    [ObservableProperty]
    private DateTime dataInicio;

    [ObservableProperty]
    private DateTime dataFim;

    [ObservableProperty]
    private bool isExportingExcel;

    public string PageTitle => "FOP Geral";
    public string IntroText => "Consulta as ocorrencias registadas durante um intervalo de datas.";
    public string PeriodoDisplay => $"Periodo selecionado: {DataInicio:dd/MM/yyyy} - {DataFim:dd/MM/yyyy}";
    public bool HasLinhas => Linhas.Count > 0;
    public string EmptyMessage => "Nao existem ocorrencias FOP para o intervalo escolhido.";
    public string ExportExcelButtonText => IsExportingExcel ? "A descarregar..." : "Descarregar Excel";
    public bool CanExportExcel => !IsExportingExcel && DataFim.Date >= DataInicio.Date;

    partial void OnDataInicioChanged(DateTime value)
    {
        OnPropertyChanged(nameof(PeriodoDisplay));
        OnPropertyChanged(nameof(CanExportExcel));
        ExportarExcelCommand.NotifyCanExecuteChanged();
    }

    partial void OnDataFimChanged(DateTime value)
    {
        OnPropertyChanged(nameof(PeriodoDisplay));
        OnPropertyChanged(nameof(CanExportExcel));
        ExportarExcelCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsExportingExcelChanged(bool value)
    {
        OnPropertyChanged(nameof(ExportExcelButtonText));
        OnPropertyChanged(nameof(CanExportExcel));
        ExportarExcelCommand.NotifyCanExecuteChanged();
    }

    public async Task LoadAsync()
    {
        await ReloadCurrentPageAsync();
    }

    [RelayCommand]
    private async Task AplicarFiltrosAsync()
    {
        ErrorMessage = string.Empty;
        await ResetToFirstPageAndReloadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanExportExcel))]
    private async Task ExportarExcelAsync()
    {
        ErrorMessage = string.Empty;

        if (DataFim.Date < DataInicio.Date)
        {
            ErrorMessage = "A data final tem de ser igual ou posterior a data inicial.";
            return;
        }

        var destinationFolder = await _destinationFolderPickerService.PickFolderAsync("Selecionar pasta para a FOP geral");
        if (string.IsNullOrWhiteSpace(destinationFolder))
            return;

        IsExportingExcel = true;

        try
        {
            var generated = await _relatoriosService.GenerateFopGeralAsync(
                DataInicio.Date,
                DataFim.Date,
                destinationFolder);

            await _dialogService.ShowSuccessAsync(
                "Excel gerado",
                $"O Excel da FOP geral foi guardado em {generated.FilePath}.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Erro", ex.Message);
        }
        finally
        {
            IsExportingExcel = false;
        }
    }

    [RelayCommand]
    private async Task VoltarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    protected override async Task LoadPageAsync()
    {
        ErrorMessage = string.Empty;

        if (DataFim.Date < DataInicio.Date)
        {
            ErrorMessage = "A data final tem de ser igual ou posterior a data inicial.";
            Linhas.Clear();
            UpdatePagination(0, 1);
            OnPropertyChanged(nameof(HasLinhas));
            return;
        }

        await ExecutePagedLoadAsync(async () =>
        {
            var pagina = await _relatoriosService.GetFopGeralAsync(DataInicio.Date, DataFim.Date, Page, PageSize);

            if (pagina is null)
            {
                ErrorMessage = "Nao foi possivel carregar a FOP geral.";
                Linhas.Clear();
                UpdatePagination(0, 1);
                OnPropertyChanged(nameof(HasLinhas));
                return;
            }

            Linhas.Clear();
            foreach (var item in pagina.Items)
                Linhas.Add(item);

            UpdatePagination(pagina.TotalItems, pagina.TotalPages);
            OnPropertyChanged(nameof(HasLinhas));
        });
    }
}
