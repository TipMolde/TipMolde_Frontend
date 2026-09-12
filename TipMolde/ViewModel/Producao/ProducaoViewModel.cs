using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere a fila operacional de pecas disponiveis para producao.
/// </summary>
public partial class ProducaoViewModel : SearchableViewModel
{
    private const string SearchModeMolde = "Molde";
    private const string SearchModePeca = "Peca";
    private const string SearchModeProximaFase = "Próxima fase";

    private readonly FasesProducaoService _fasesProducaoService;
    private readonly PecasService _pecasService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly UtilizadoresService _utilizadoresService;
    private readonly RegistosProducaoService _registosProducaoService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private List<FaseProducaoItem> _todasFases = [];

    /// <summary>
    /// Construtor do view model da pagina de producao.
    /// </summary>
    public ProducaoViewModel(
        FasesProducaoService fasesProducaoService,
        PecasService pecasService,
        SessaoPersistidaService sessaoPersistidaService,
        UtilizadoresService utilizadoresService,
        RegistosProducaoService registosProducaoService,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        _fasesProducaoService = fasesProducaoService;
        _pecasService = pecasService;
        _sessaoPersistidaService = sessaoPersistidaService;
        _utilizadoresService = utilizadoresService;
        _registosProducaoService = registosProducaoService;
        _dialogService = dialogService;
        _navigationService = navigationService;

        PageSize = 10;
        SelectedSearchModeIndex = 0;
    }

    public ObservableCollection<ProducaoPecaDisponivelItem> PecasDisponiveis { get; } = new();
    public IReadOnlyList<string> SearchModes { get; } = [SearchModeMolde, SearchModePeca, SearchModeProximaFase];

    [ObservableProperty]
    private int selectedSearchModeIndex;

    [ObservableProperty]
    private int? gestorProducaoId;

    [ObservableProperty]
    private string gestorProducaoNome = string.Empty;

    [ObservableProperty]
    private bool isUpdatingPlaneamento;

    public bool HasPecasDisponiveis => PecasDisponiveis.Count > 0;
    public static string EmptyPecasMessage => "Nao existem pecas disponiveis para trabalhar neste momento.";
    public string GestorProducaoDisplay => GetGestorProducaoDisplay();
    public string AlterarFasePlaneadaButtonText => IsUpdatingPlaneamento ? "A alterar..." : "Alterar fase planeada";
    public bool CanAlterarFasePlaneadaGlobal => !IsUpdatingPlaneamento;

    partial void OnSelectedSearchModeIndexChanged(int value)
    {
        if (value < 0 || value >= SearchModes.Count)
            return;

        _ = ResetToFirstPageAndReloadAsync();
    }

    partial void OnGestorProducaoIdChanged(int? value)
    {
        OnPropertyChanged(nameof(GestorProducaoDisplay));
    }

    partial void OnGestorProducaoNomeChanged(string value)
    {
        OnPropertyChanged(nameof(GestorProducaoDisplay));
    }

    partial void OnIsUpdatingPlaneamentoChanged(bool value)
    {
        OnPropertyChanged(nameof(AlterarFasePlaneadaButtonText));
        OnPropertyChanged(nameof(CanAlterarFasePlaneadaGlobal));
        AlterarFasePlaneadaCommand.NotifyCanExecuteChanged();
    }

    public async Task LoadAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            var gestor = await GetGestorProducaoAtualAsync();
            GestorProducaoId = gestor?.User_id;
            GestorProducaoNome = gestor?.Nome ?? string.Empty;

            await ReloadCurrentPageAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            PecasDisponiveis.Clear();
            UpdatePagination(0, 1);
            OnPropertyChanged(nameof(HasPecasDisponiveis));
        }
    }

    protected override async Task LoadPageAsync()
    {
        ErrorMessage = string.Empty;

        await ExecutePagedLoadAsync(async () =>
        {
            // Picker can briefly have no selection while its items are being bound.
            var searchMode = SelectedSearchModeIndex >= 0 && SelectedSearchModeIndex < SearchModes.Count
                ? SearchModes[SelectedSearchModeIndex]
                : SearchModeMolde;
            var pagina = await _pecasService.GetFilaTrabalhoAsync(Page, PageSize, SearchTerm, searchMode);

            if (pagina is null)
            {
                ErrorMessage = "Nao foi possivel carregar a fila de trabalho das pecas.";
                PecasDisponiveis.Clear();
                UpdatePagination(0, 1);
                OnPropertyChanged(nameof(HasPecasDisponiveis));
                return;
            }

            PecasDisponiveis.Clear();
            foreach (var item in pagina.Items)
                PecasDisponiveis.Add(item);

            UpdatePagination(pagina.TotalItems, pagina.TotalPages);
            OnPropertyChanged(nameof(HasPecasDisponiveis));
        });
    }

    [RelayCommand]
    private async Task AbrirFilaTrabalhoAsync()
    {
        await _navigationService.GoToAsync(nameof(FilaTrabalhoPage));
    }

    [RelayCommand]
    private async Task AbrirRegistoAtivoAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            if (!GestorProducaoId.HasValue)
            {
                await _dialogService.ShowInfoAsync("GestorProducao", "Nao foi possivel identificar o gestor de producao autenticado.");
                return;
            }

            var registoAtivo = await GetRegistoAtivoDoGestorProducaoAsync(GestorProducaoId.Value);
            if (registoAtivo is null)
            {
                await _dialogService.ShowInfoAsync("Registo ativo", "Nao tens nenhum registo ativo neste momento.");
                return;
            }

            await _navigationService.GoToAsync(nameof(RegistoProducaoPage), new Dictionary<string, object>
            {
                ["abrir_ativo"] = true
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task SelecionarPecaAsync(ProducaoPecaDisponivelItem? item)
    {
        if (item is null)
            return;

        ErrorMessage = string.Empty;

        try
        {
            await _navigationService.GoToAsync(nameof(RegistoProducaoPage), new Dictionary<string, object>
            {
                ["peca_contexto"] = item
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAlterarFasePlaneada))]
    private async Task AlterarFasePlaneadaAsync(ProducaoPecaDisponivelItem? item)
    {
        if (item is null)
            return;

        ErrorMessage = string.Empty;
        IsUpdatingPlaneamento = true;

        try
        {
            await EnsureFasesLoadedAsync();

            if (_todasFases.Count == 0)
            {
                await _dialogService.ShowInfoAsync("Fase planeada", "Nao foi possivel carregar as fases de producao.");
                return;
            }

            var selecionada = await SelecionarFaseAsync(item);
            if (selecionada is null)
                return;

            if (item.ProximaFaseId.HasValue && item.ProximaFaseId.Value == selecionada.FasesProducao_id)
            {
                await _dialogService.ShowInfoAsync(
                    "Fase planeada",
                    $"A peca {item.DesignacaoDisplay} ja esta planeada para {selecionada.NomeDisplay}.");
                return;
            }

            await _pecasService.UpdateProximaFaseAsync(item.PecaId, selecionada.FasesProducao_id);

            await _dialogService.ShowSuccessAsync(
                "Fase planeada atualizada",
                $"A peca {item.DesignacaoDisplay} passou a apontar para {selecionada.NomeDisplay}.");

            await ReloadCurrentPageAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsUpdatingPlaneamento = false;
            AlterarFasePlaneadaCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanAlterarFasePlaneada(ProducaoPecaDisponivelItem? item)
    {
        return item is not null && CanAlterarFasePlaneadaGlobal;
    }

    private string GetGestorProducaoDisplay()
    {
        if (!GestorProducaoId.HasValue)
            return "Sessao sem gestor de producao identificado";

        var gestorNome = string.IsNullOrWhiteSpace(GestorProducaoNome)
            ? "Gestor de producao autenticado"
            : GestorProducaoNome;

        return gestorNome;
    }

    private async Task<UtilizadorDto?> GetGestorProducaoAtualAsync()
    {
        var userId = _sessaoPersistidaService.TryGetCurrentUserId();
        if (!userId.HasValue)
            return null;

        try
        {
            return await _utilizadoresService.GetUtilizadorByIdAsync(userId.Value);
        }
        catch
        {
            return new UtilizadorDto
            {
                User_id = userId.Value,
                Nome = string.Empty
            };
        }
    }

    private async Task<RegistoProducaoDto?> GetRegistoAtivoDoGestorProducaoAsync(int gestorProducaoId)
    {
        var all = await GetAllRegistosAsync();

        return all
            .GroupBy(item => new { item.PecaId, item.FaseId })
            .Select(group => group.OrderByDescending(item => item.DataHora).First())
            .Where(item => item.GestorProducaoId == gestorProducaoId && EstadoContaComoAtivo(item.EstadoProducao))
            .OrderByDescending(item => item.DataHora)
            .FirstOrDefault();
    }

    private async Task<List<RegistoProducaoDto>> GetAllRegistosAsync()
    {
        var primeiraPagina = await _registosProducaoService.GetAllAsync(1, 100);
        if (primeiraPagina is null)
            return [];

        var registos = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _registosProducaoService.GetAllAsync(page, 100);
            if (pagina?.Items is null)
                continue;

            registos.AddRange(pagina.Items);
        }

        return registos;
    }

    private async Task EnsureFasesLoadedAsync()
    {
        if (_todasFases.Count > 0)
            return;

        var primeiraPagina = await _fasesProducaoService.GetAllAsync(1, 100);
        if (primeiraPagina?.Items is null)
            return;

        _todasFases = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _fasesProducaoService.GetAllAsync(page, 100);
            if (pagina?.Items is null)
                continue;

            _todasFases.AddRange(pagina.Items);
        }

        _todasFases = _todasFases
            .OrderBy(item => GetPhaseSortOrder(item.Nome))
            .ThenBy(item => item.FasesProducao_id)
            .ToList();
    }

    private async Task<FaseProducaoItem?> SelecionarFaseAsync(ProducaoPecaDisponivelItem item)
    {
        var opcoes = _todasFases.Select(fase => fase.NomeDisplay).ToArray();
        var selecionada = await _dialogService.ShowSelectionAsync(
            $"Alterar fase planeada de {item.DesignacaoDisplay}",
            "Cancelar",
            opcoes);

        if (string.IsNullOrWhiteSpace(selecionada))
            return null;

        return _todasFases.FirstOrDefault(fase => fase.NomeDisplay == selecionada);
    }

    private static int GetPhaseSortOrder(string? nome)
    {
        return string.Equals(nome?.Trim(), "MAQUINACAO", StringComparison.OrdinalIgnoreCase)
            ? 0
            : string.Equals(nome?.Trim(), "EROSAO", StringComparison.OrdinalIgnoreCase)
                ? 1
                : string.Equals(nome?.Trim(), "MONTAGEM", StringComparison.OrdinalIgnoreCase)
                    ? 2
                    : 99;
    }

    private static bool EstadoContaComoAtivo(string? estado)
    {
        return string.Equals(estado, "PREPARACAO", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(estado, "EM_CURSO", StringComparison.OrdinalIgnoreCase);
    }
}
