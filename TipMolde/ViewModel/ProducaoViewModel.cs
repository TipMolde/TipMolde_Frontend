using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

public partial class ProducaoViewModel : SearchableViewModel
{
    private const string SearchModeMolde = "Molde";
    private const string SearchModePeca = "Peca";

    private readonly PecasService _pecasService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly UtilizadoresService _utilizadoresService;
    private readonly RegistosProducaoService _registosProducaoService;
    private readonly IDialogService _dialogService;

    public ProducaoViewModel(
        PecasService pecasService,
        SessaoPersistidaService sessaoPersistidaService,
        UtilizadoresService utilizadoresService,
        RegistosProducaoService registosProducaoService,
        IDialogService dialogService)
    {
        _pecasService = pecasService;
        _sessaoPersistidaService = sessaoPersistidaService;
        _utilizadoresService = utilizadoresService;
        _registosProducaoService = registosProducaoService;
        _dialogService = dialogService;

        PageSize = 8;
        SelectedSearchModeIndex = 0;
    }

    public ObservableCollection<ProducaoPecaDisponivelItem> PecasDisponiveis { get; } = new();
    public IReadOnlyList<string> SearchModes { get; } = [SearchModeMolde, SearchModePeca];

    [ObservableProperty]
    private int selectedSearchModeIndex;

    [ObservableProperty]
    private int? gestorProducaoId;

    [ObservableProperty]
    private string gestorProducaoNome = string.Empty;

    public bool HasPecasDisponiveis => PecasDisponiveis.Count > 0;
    public static string EmptyPecasMessage => "Nao existem pecas disponiveis para trabalhar neste momento.";
    public string GestorProducaoDisplay => GetGestorProducaoDisplay();

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
            var searchMode = SearchModes[SelectedSearchModeIndex];
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
        await Shell.Current.GoToAsync(nameof(FilaTrabalhoPage));
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

            await Shell.Current.GoToAsync(nameof(RegistoProducaoPage), new Dictionary<string, object>
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
            await Shell.Current.GoToAsync(nameof(RegistoProducaoPage), new Dictionary<string, object>
            {
                ["peca_contexto"] = item
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private string GetGestorProducaoDisplay()
    {
        if (!GestorProducaoId.HasValue)
            return "Sessao sem gestor de producao identificado";

        var gestorNome = string.IsNullOrWhiteSpace(GestorProducaoNome)
            ? $"Gestor de producao #{GestorProducaoId}"
            : GestorProducaoNome;

        return $"{gestorNome} (#{GestorProducaoId})";
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

    private static bool EstadoContaComoAtivo(string? estado)
    {
        return string.Equals(estado, "PREPARACAO", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(estado, "EM_CURSO", StringComparison.OrdinalIgnoreCase);
    }
}
