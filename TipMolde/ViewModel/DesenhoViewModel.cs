using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

public partial class DesenhoViewModel : PaginatedViewModel
{
    private readonly EncomendasService _encomendasService;
    private readonly PecasService _pecasService;
    private readonly IDialogService _dialogService;
    private readonly List<DesenhoMoldeItem> _todosMoldes = [];
    private readonly List<DesenhoMoldeItem> _moldesFiltrados = [];

    public DesenhoViewModel(
        EncomendasService encomendasService,
        PecasService pecasService,
        IDialogService dialogService)
    {
        _encomendasService = encomendasService;
        _pecasService = pecasService;
        _dialogService = dialogService;
        PageSize = 8;
    }

    public ObservableCollection<DesenhoMoldeItem> Moldes { get; } = new();

    [ObservableProperty]
    private string searchTerm = string.Empty;

    public bool HasMoldes => Moldes.Count > 0;
    public int TotalMoldes => _moldesFiltrados.Count;
    public int TotalEncomendasConfirmadas => _moldesFiltrados.Select(item => item.EncomendaId).Distinct().Count();
    public string EmptyMessage => string.IsNullOrWhiteSpace(SearchTerm)
        ? "Nao existem moldes com projeto concluido e revisao aprovada para desenho."
        : "Nenhum molde corresponde aos filtros atuais.";

    partial void OnSearchTermChanged(string value)
    {
        if (Page != 1)
            Page = 1;

        RefreshMoldes();
        OnPropertyChanged(nameof(EmptyMessage));
    }

    public async Task LoadAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            await ExecutePagedLoadAsync(async () =>
            {
                _todosMoldes.Clear();

                var associacoes = await GetAllEncomendasConfirmadasParaDesenhoAsync();
                if (associacoes.Count == 0)
                {
                    RefreshMoldes();
                    return;
                }

                var candidatos = await Task.WhenAll(associacoes
                    .Select(async associacao => await BuildDesenhoMoldeItemAsync(associacao)));

                _todosMoldes.AddRange(candidatos
                    .Where(item => item is not null)
                    .Select(item => item!)
                    .OrderBy(item => item.DataEntregaPrevista == default ? DateTime.MaxValue : item.DataEntregaPrevista)
                    .ThenBy(item => item.NumeroEncomendaDisplay)
                    .ThenBy(item => item.NumeroMoldeDisplay));

                Page = 1;
                RefreshMoldes();
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Moldes.Clear();
            OnPropertyChanged(nameof(HasMoldes));
            OnPropertyChanged(nameof(EmptyMessage));
        }
    }

    protected override Task LoadPageAsync()
    {
        RefreshMoldes();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RecarregarAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private static async Task VoltarAsync()
    {
        await ShellNavigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task AbrirMoldeAsync(DesenhoMoldeItem? molde)
    {
        if (molde is null || molde.MoldeId <= 0)
            return;

        await Shell.Current.GoToAsync($"{nameof(MoldeDetalhePage)}?molde_id={molde.MoldeId}");
    }

    [RelayCommand]
    private async Task InserirPecasAsync(DesenhoMoldeItem? molde)
    {
        if (molde is null)
            return;

        var selection = await _dialogService.ShowSelectionAsync(
            $"Inserir pecas para o molde {molde.NumeroMoldeDisplay}",
            "Cancelar",
            "Importar CSV",
            "Criar manualmente");

        if (string.IsNullOrWhiteSpace(selection))
            return;

        if (string.Equals(selection, "Importar CSV", StringComparison.Ordinal))
        {
            await ImportarPecasCsvAsync(molde);
            return;
        }

        var numeroMolde = Uri.EscapeDataString(molde.NumeroMoldeDisplay);
        await Shell.Current.GoToAsync($"{nameof(AdicionarPecaPage)}?molde_id={molde.MoldeId}&numero_molde={numeroMolde}");
    }

    private async Task<List<EncomendaMoldeDto>> GetAllEncomendasConfirmadasParaDesenhoAsync()
    {
        var primeiraPagina = await _encomendasService.GetEncomendasConfirmadasParaDesenhoAsync(1, 100);
        if (primeiraPagina is null)
            return [];

        var associacoes = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _encomendasService.GetEncomendasConfirmadasParaDesenhoAsync(page, 100);
            if (pagina?.Items is null)
                continue;

            associacoes.AddRange(pagina.Items);
        }

        return associacoes;
    }

    private async Task<DesenhoMoldeItem?> BuildDesenhoMoldeItemAsync(EncomendaMoldeDto associacao)
    {
        if (associacao.Molde_id <= 0)
            return null;

        var paginaPecas = await _pecasService.GetByMoldeIdAsync(associacao.Molde_id, 1, 100);
        var pecas = paginaPecas?.Items ?? [];

        return new DesenhoMoldeItem
        {
            EncomendaId = associacao.Encomenda_id,
            MoldeId = associacao.Molde_id,
            TotalPecas = paginaPecas?.TotalItems ?? 0,
            NumeroEncomendaCliente = associacao.NumeroEncomendaCliente,
            NumeroMolde = associacao.NumeroMolde,
            DataEntregaPrevista = associacao.DataEntregaPrevista == default ? null : associacao.DataEntregaPrevista,
            PecasResumoDisplay = BuildPecasResumoDisplay(pecas)
        };
    }

    private static string BuildPecasResumoDisplay(IEnumerable<PecaDto> pecas)
    {
        var resumo = pecas
            .Take(3)
            .Select(BuildPecaResumo)
            .ToList();

        if (resumo.Count == 0)
            return "Sem detalhes adicionais das pecas.";

        var texto = string.Join(" | ", resumo);
        var restantes = pecas.Skip(resumo.Count).Any();

        return restantes ? $"{texto} | e mais pecas associadas" : texto;
    }

    private static string BuildPecaResumo(PecaDto peca)
    {
        var numero = string.IsNullOrWhiteSpace(peca.NumeroPeca) ? $"Peca #{peca.PecaId}" : peca.NumeroPeca;
        var designacao = string.IsNullOrWhiteSpace(peca.Designacao) ? "Sem designacao" : peca.Designacao;
        var quantidade = Math.Max(0, peca.Quantidade);

        return $"{numero} - {designacao} ({quantidade})";
    }

    private void RefreshMoldes()
    {
        IEnumerable<DesenhoMoldeItem> query = _todosMoldes;

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim();
            query = query.Where(item =>
                item.NumeroMoldeDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.NomeMoldeDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.NumeroEncomendaDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.NomeClienteDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.TotalPecasDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.DataEntregaPrevistaDisplay.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        _moldesFiltrados.Clear();
        _moldesFiltrados.AddRange(query.OrderBy(item => item.DataEntregaPrevista ?? DateTime.MaxValue).ThenBy(item => item.NumeroMoldeDisplay));

        var totalFiltrado = _moldesFiltrados.Count;
        var totalPaginas = totalFiltrado <= 0 ? 1 : (int)Math.Ceiling((double)totalFiltrado / PageSize);
        UpdatePagination(totalFiltrado, totalPaginas);

        var paginaAtual = _moldesFiltrados
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        Moldes.Clear();
        foreach (var molde in paginaAtual)
            Moldes.Add(molde);

        OnPropertyChanged(nameof(HasMoldes));
        OnPropertyChanged(nameof(TotalMoldes));
        OnPropertyChanged(nameof(TotalEncomendasConfirmadas));
        OnPropertyChanged(nameof(EmptyMessage));
    }

    private async Task ImportarPecasCsvAsync(DesenhoMoldeItem molde)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = $"Selecionar CSV para o molde {molde.NumeroMoldeDisplay}",
                FileTypes = CsvFileTypes
            });

            if (file is null)
                return;

            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                await _dialogService.ShowErrorAsync(
                    "Ficheiro invalido",
                    "Selecione um ficheiro CSV para importar as pecas.");
                return;
            }

            var result = await _pecasService.ImportCsvAsync(molde.MoldeId, file);
            if (result is null)
            {
                await _dialogService.ShowErrorAsync(
                    "Importacao de pecas",
                    "A importacao terminou sem devolver um resumo.");
                return;
            }

            await _dialogService.ShowSuccessAsync(
                "Importacao concluida",
                $"Foram lidas {result.TotalLinhasPecaLidas} linhas e consolidadas {result.TotalPecasConsolidadas} pecas para o molde {molde.NumeroMoldeDisplay}.");

            await AtualizarTotalPecasMoldeAsync(molde.MoldeId);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "Importacao de pecas",
                ex.Message);
        }
    }

    private async Task AtualizarTotalPecasMoldeAsync(int moldeId)
    {
        var totais = await GetTotaisPecasPorMoldeAsync([moldeId]);
        if (!totais.TryGetValue(moldeId, out var totalPecasAtual))
            return;

        var molde = _todosMoldes.First(item => item.MoldeId == moldeId);
        molde.TotalPecas = totalPecasAtual;
        RefreshMoldes();
    }

    private async Task<Dictionary<int, int>> GetTotaisPecasPorMoldeAsync(IEnumerable<int> moldeIds)
    {
        var moldesValidos = moldeIds
            .Where(item => item > 0)
            .Distinct()
            .ToList();

        var totais = await Task.WhenAll(moldesValidos.Select(async moldeId =>
        {
            var pagina = await _pecasService.GetByMoldeIdAsync(moldeId, 1, 1);
            return new
            {
                MoldeId = moldeId,
                Total = pagina?.TotalItems ?? 0
            };
        }));

        return totais.ToDictionary(item => item.MoldeId, item => item.Total);
    }

    private static readonly FilePickerFileType CsvFileTypes = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.WinUI] = [".csv"],
        [DevicePlatform.Android] = ["text/csv", "text/comma-separated-values", "application/csv", "application/vnd.ms-excel"],
        [DevicePlatform.iOS] = ["public.comma-separated-values-text", "public.plain-text"],
        [DevicePlatform.MacCatalyst] = ["public.comma-separated-values-text", "public.plain-text"]
    });
}
