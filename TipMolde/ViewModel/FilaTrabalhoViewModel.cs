using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

public partial class FilaTrabalhoViewModel : PaginatedViewModel
{
    private readonly EncomendasService _encomendasService;
    private readonly MoldesService _moldesService;

    public FilaTrabalhoViewModel(
        EncomendasService encomendasService,
        MoldesService moldesService)
    {
        _encomendasService = encomendasService;
        _moldesService = moldesService;
    }

    public ObservableCollection<FilaGlobalMoldeItemDto> Moldes { get; } = new();

    public bool HasItems => Moldes.Count > 0;
    public string EmptyMessage => "Nao existem moldes na fila global de trabalho.";

    public async Task LoadFilaAsync()
    {
        await ReloadCurrentPageAsync();
    }

    protected override async Task LoadPageAsync()
    {
        ErrorMessage = string.Empty;

        await ExecutePagedLoadAsync(async () =>
        {
            var result = await _encomendasService.GetFilaGlobalMoldeAsync(Page, PageSize);

            if (result is null)
            {
                ErrorMessage = "Nao foi possivel carregar a fila global de trabalho.";
                Moldes.Clear();
                UpdatePagination(0, 1);
                NotifyCollectionStateChanged();
                return;
            }

            var imagensPorMoldeId = new Dictionary<int, string>();
            var moldesSemImagem = result.Items
                .Where(item => string.IsNullOrWhiteSpace(item.ImagemCapaPath) && item.MoldeId > 0)
                .Select(item => item.MoldeId)
                .Distinct()
                .ToList();

            if (moldesSemImagem.Count > 0)
            {
                var moldes = await Task.WhenAll(moldesSemImagem.Select(moldeId => _moldesService.GetByIdAsync(moldeId)));
                foreach (var molde in moldes.Where(molde => molde is not null))
                    imagensPorMoldeId[molde!.MoldeId] = molde.ImagemCapaPath ?? string.Empty;
            }

            Moldes.Clear();

            foreach (var item in result.Items
                         .OrderBy(molde => molde.Prioridade)
                         .ThenBy(molde => molde.DataEntregaPrevista)
                         .ThenBy(molde => molde.MoldeId))
            {
                if (string.IsNullOrWhiteSpace(item.ImagemCapaPath) &&
                    imagensPorMoldeId.TryGetValue(item.MoldeId, out var imagemCapaPath))
                    item.ImagemCapaPath = imagemCapaPath;

                Moldes.Add(item);
            }

            UpdatePagination(result.TotalItems, result.TotalPages);
            NotifyCollectionStateChanged();
        });
    }

    [RelayCommand]
    private async Task VoltarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task AbrirMoldeAsync(FilaGlobalMoldeItemDto? item)
    {
        if (item is null || item.MoldeId <= 0)
            return;

        await Shell.Current.GoToAsync($"{nameof(MoldeDetalhePage)}?molde_id={item.MoldeId}");
    }

    private void NotifyCollectionStateChanged()
    {
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(EmptyMessage));
    }
}
