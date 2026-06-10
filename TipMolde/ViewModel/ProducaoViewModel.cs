using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

public partial class ProducaoViewModel : SearchableViewModel
{
    private const string SearchModeMolde = "Molde";
    private const string SearchModePeca = "Peca";
    private const string SearchModeFase = "Fase";

    private readonly EncomendasService _encomendasService;
    private readonly PecasService _pecasService;
    private readonly FasesProducaoService _fasesProducaoService;
    private readonly MaquinasService _maquinasService;
    private readonly RegistosProducaoService _registosProducaoService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly UtilizadoresService _utilizadoresService;
    private readonly IDialogService _dialogService;

    private readonly List<ProducaoPecaDisponivelItem> _todasPecasDisponiveis = [];
    private List<FaseProducaoItem> _todasFases = [];
    private List<MaquinaItem> _todasMaquinas = [];

    public ProducaoViewModel(
        EncomendasService encomendasService,
        PecasService pecasService,
        FasesProducaoService fasesProducaoService,
        MaquinasService maquinasService,
        RegistosProducaoService registosProducaoService,
        SessaoPersistidaService sessaoPersistidaService,
        UtilizadoresService utilizadoresService,
        IDialogService dialogService)
    {
        _encomendasService = encomendasService;
        _pecasService = pecasService;
        _fasesProducaoService = fasesProducaoService;
        _maquinasService = maquinasService;
        _registosProducaoService = registosProducaoService;
        _sessaoPersistidaService = sessaoPersistidaService;
        _utilizadoresService = utilizadoresService;
        _dialogService = dialogService;

        PageSize = 8;
        SelectedSearchModeIndex = 0;
    }

    public ObservableCollection<ProducaoPecaDisponivelItem> PecasDisponiveis { get; } = new();
    public ObservableCollection<FaseProducaoItem> FasesRegistoDisponiveis { get; } = new();
    public ObservableCollection<EstadoProducaoOption> EstadosRegistoDisponiveis { get; } = new();
    public ObservableCollection<MaquinaItem> MaquinasRegistoDisponiveis { get; } = new();
    public IReadOnlyList<string> SearchModes { get; } = [SearchModeMolde, SearchModePeca, SearchModeFase];

    [ObservableProperty]
    private int selectedSearchModeIndex;

    [ObservableProperty]
    private ProducaoPecaDisponivelItem? selectedPeca;

    [ObservableProperty]
    private FaseProducaoItem? selectedFase;

    [ObservableProperty]
    private EstadoProducaoOption? selectedEstadoRegisto;

    [ObservableProperty]
    private MaquinaItem? selectedMaquina;

    [ObservableProperty]
    private bool isSavingRegisto;

    [ObservableProperty]
    private string registoErrorMessage = string.Empty;

    [ObservableProperty]
    private int? gestorProducaoId;

    [ObservableProperty]
    private string gestorProducaoNome = string.Empty;

    public bool HasPecasDisponiveis => PecasDisponiveis.Count > 0;
    public bool HasSelectedPeca => SelectedPeca is not null;
    public bool HasRegistoError => !string.IsNullOrWhiteSpace(RegistoErrorMessage);
    public bool HasMaquinasRegistoDisponiveis => MaquinasRegistoDisponiveis.Count > 0;
    public bool IsMachineSelectionVisible => SelectedEstadoRegisto is not null && EstadoRequerMaquina(SelectedEstadoRegisto.Value);
    public string EmptyPecasMessage => "Nao existem pecas disponiveis para trabalhar neste momento.";
    public string GestorProducaoDisplay => GestorProducaoId.HasValue
        ? $"{(string.IsNullOrWhiteSpace(GestorProducaoNome) ? $"Gestor de producao #{GestorProducaoId}" : GestorProducaoNome)} (#{GestorProducaoId})"
        : "Sessao sem gestor de producao identificado";
    public string PecaSelecionadaResumo => SelectedPeca is null
        ? "Seleciona uma peca da lista para preparar o registo."
        : $"{SelectedPeca.DesignacaoDisplay} | {SelectedPeca.NumeroMoldeDisplay} | {SelectedPeca.PrioridadeResumo}";
    public string EstadoSelecionadoResumo => SelectedEstadoRegisto?.DisplayName ?? "Seleciona uma fase para ver o proximo estado permitido.";
    public string FaseSelecionadaResumo => SelectedFase?.NomeDisplay ?? "Seleciona uma fase.";
    public string ResumoFasesPecaSelecionada => SelectedPeca?.ResumoFases ?? "Sem historico para apresentar.";
    public string RegistarButtonText => IsSavingRegisto ? "A registar..." : "Registar producao";
    public string MaquinasHint => !IsMachineSelectionVisible
        ? "Esta transicao nao exige maquina."
        : MaquinasRegistoDisponiveis.Count == 0
            ? "Nao ha maquinas configuradas para a fase selecionada, mas podes continuar sem maquina."
            : "Seleciona uma maquina disponivel ou usa Nenhuma Maquina para trabalho manual.";
    public bool CanRegistarProducao =>
        !IsSavingRegisto &&
        !IsLoading &&
        SelectedPeca is not null &&
        SelectedFase is not null &&
        SelectedEstadoRegisto is not null &&
        GestorProducaoId.HasValue;

    partial void OnSelectedSearchModeIndexChanged(int value)
    {
        if (_todasPecasDisponiveis.Count == 0)
            return;

        _ = ResetToFirstPageAndReloadAsync();
    }

    partial void OnSelectedPecaChanged(ProducaoPecaDisponivelItem? value)
    {
        OnPropertyChanged(nameof(HasSelectedPeca));
        OnPropertyChanged(nameof(PecaSelecionadaResumo));
        OnPropertyChanged(nameof(ResumoFasesPecaSelecionada));
        AtualizarFasesRegistoDisponiveis();
        RegistarProducaoCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedFaseChanged(FaseProducaoItem? value)
    {
        OnPropertyChanged(nameof(FaseSelecionadaResumo));
        AtualizarEstadosRegistoDisponiveis();
        RegistarProducaoCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedEstadoRegistoChanged(EstadoProducaoOption? value)
    {
        OnPropertyChanged(nameof(EstadoSelecionadoResumo));
        OnPropertyChanged(nameof(IsMachineSelectionVisible));
        AtualizarMaquinasDisponiveis();
        RegistarProducaoCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedMaquinaChanged(MaquinaItem? value)
    {
        RegistarProducaoCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSavingRegistoChanged(bool value)
    {
        OnPropertyChanged(nameof(RegistarButtonText));
        RegistarProducaoCommand.NotifyCanExecuteChanged();
    }

    partial void OnRegistoErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasRegistoError));
    partial void OnGestorProducaoIdChanged(int? value)
    {
        OnPropertyChanged(nameof(GestorProducaoDisplay));
        RegistarProducaoCommand.NotifyCanExecuteChanged();
    }

    partial void OnGestorProducaoNomeChanged(string value) => OnPropertyChanged(nameof(GestorProducaoDisplay));

    public async Task LoadAsync()
    {
        var previouslySelectedPecaId = SelectedPeca?.PecaId;
        ErrorMessage = string.Empty;
        RegistoErrorMessage = string.Empty;

        try
        {
            await ExecutePagedLoadAsync(async () =>
            {
                var filaGlobalTask = GetAllFilaGlobalMoldeAsync();
                var fasesTask = GetAllFasesAsync();
                var maquinasTask = GetAllMaquinasAsync();
                var gestorProducaoTask = GetGestorProducaoAtualAsync();

                await Task.WhenAll(filaGlobalTask, fasesTask, maquinasTask, gestorProducaoTask);

                _todasFases = fasesTask.Result;
                _todasMaquinas = maquinasTask.Result;

                GestorProducaoId = gestorProducaoTask.Result?.User_id;
                GestorProducaoNome = gestorProducaoTask.Result?.Nome ?? string.Empty;

                var pecasDisponiveis = await ConstruirPecasDisponiveisAsync(filaGlobalTask.Result, _todasFases);

                _todasPecasDisponiveis.Clear();
                _todasPecasDisponiveis.AddRange(pecasDisponiveis
                    .OrderBy(item => item.PrioridadeMolde)
                    .ThenBy(item => item.PrioridadePeca)
                    .ThenBy(item => item.DataEntregaPrevista <= DateTime.MinValue ? DateTime.MaxValue : item.DataEntregaPrevista)
                    .ThenBy(item => item.NumeroMoldeDisplay)
                    .ThenBy(item => item.NumeroPecaDisplay)
                    .ThenBy(item => item.DesignacaoDisplay));

                Page = 1;
                AplicarPaginacaoLista();
                RestaurarSelecao(previouslySelectedPecaId);
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            LimparLista();
        }
    }

    protected override Task LoadPageAsync()
    {
        AplicarPaginacaoLista();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task AbrirFilaTrabalhoAsync()
    {
        await Shell.Current.GoToAsync(nameof(FilaTrabalhoPage));
    }

    [RelayCommand]
    private async Task AbrirRegistoAtivoAsync()
    {
        RegistoErrorMessage = string.Empty;

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
            RegistoErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task SelecionarPecaAsync(ProducaoPecaDisponivelItem? item)
    {
        if (item is null)
            return;

        RegistoErrorMessage = string.Empty;

        try
        {
            await Shell.Current.GoToAsync(nameof(RegistoProducaoPage), new Dictionary<string, object>
            {
                ["peca_contexto"] = item
            });
        }
        catch (Exception ex)
        {
            RegistoErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private Task LimparSelecaoAsync()
    {
        SelectedPeca = null;
        SelectedFase = null;
        SelectedEstadoRegisto = null;
        SelectedMaquina = null;
        RegistoErrorMessage = string.Empty;
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanRegistarProducao))]
    private async Task RegistarProducaoAsync()
    {
        if (!CanRegistarProducao || SelectedPeca is null || SelectedFase is null || SelectedEstadoRegisto is null || !GestorProducaoId.HasValue)
            return;

        IsSavingRegisto = true;
        RegistoErrorMessage = string.Empty;

        try
        {
            await _registosProducaoService.CreateAsync(
                SelectedPeca.PecaId,
                SelectedFase.FasesProducao_id,
                GestorProducaoId.Value,
                SelectedEstadoRegisto.Value,
                IsMachineSelectionVisible && SelectedMaquina?.Maquina_id > 0 ? SelectedMaquina.Maquina_id : null,
                SelectedPeca.ProximaFaseId);

            await _dialogService.ShowSuccessAsync(
                "Producao registada",
                $"Foi registado o estado {SelectedEstadoRegisto.DisplayName} para a peca {SelectedPeca.DesignacaoDisplay} na fase {SelectedFase.NomeDisplay}.");

            await LoadAsync();
        }
        catch (Exception ex)
        {
            RegistoErrorMessage = ex.Message;
        }
        finally
        {
            IsSavingRegisto = false;
        }
    }

    private async Task<List<ProducaoPecaDisponivelItem>> ConstruirPecasDisponiveisAsync(
        IReadOnlyCollection<FilaGlobalMoldeItemDto> filaGlobal,
        IReadOnlyCollection<FaseProducaoItem> fases)
    {
        var moldesPrioritarios = filaGlobal
            .GroupBy(item => item.MoldeId)
            .Select(group => group
                .OrderBy(item => item.Prioridade)
                .ThenBy(item => item.DataEntregaPrevista <= DateTime.MinValue ? DateTime.MaxValue : item.DataEntregaPrevista)
                .First())
            .ToList();

        var porMoldeTasks = moldesPrioritarios.Select(async molde =>
        {
            var pecas = await GetAllPecasByMoldeIdAsync(molde.MoldeId);
            var pecasElegiveis = pecas.Where(peca => peca.MaterialRecebido).ToList();

            var itemTasks = pecasElegiveis.Select(async peca =>
            {
                var ultimosRegistos = await GetUltimosRegistosPorFaseAsync(peca.PecaId, fases);

                if (EstaASerTrabalhadaAgora(ultimosRegistos))
                    return null;

                var fasePlaneada = ResolveFasePlaneada(peca.ProximaFase_id, fases, ultimosRegistos);
                if (fasePlaneada is null)
                    return null;

                if (GetEstadosDisponiveis(ultimosRegistos, fasePlaneada).Count == 0)
                    return null;

                var ultimoGlobal = ultimosRegistos.Values
                    .Where(item => item is not null)
                    .OrderByDescending(item => item!.DataHora)
                    .FirstOrDefault();

                return new ProducaoPecaDisponivelItem
                {
                    MoldeId = molde.MoldeId,
                    PecaId = peca.PecaId,
                    PrioridadeMolde = molde.Prioridade,
                    PrioridadePeca = peca.Prioridade,
                    Quantidade = peca.Quantidade,
                    NumeroMolde = molde.NumeroMolde,
                    NomeMolde = molde.NomeMolde,
                    NumeroEncomendaCliente = molde.NumeroEncomendaCliente,
                    NomeCliente = molde.NomeCliente,
                    Designacao = peca.Designacao,
                    NumeroPeca = peca.NumeroPeca,
                    DataEntregaPrevista = molde.DataEntregaPrevista,
                    UltimoEstadoGlobal = ultimoGlobal?.EstadoProducao ?? string.Empty,
                    UltimaFaseGlobal = ultimoGlobal is null
                        ? "sem fase"
                        : GetNomeFaseDisplay(ultimosRegistos.Keys.FirstOrDefault(id => ultimosRegistos[id] == ultimoGlobal)),
                    ProximaFaseId = fasePlaneada.FasesProducao_id,
                    ProximaFaseNome = fasePlaneada.NomeDisplay,
                    FaseTrabalho = fasePlaneada.NomeDisplay,
                    ProximoPasso = GetProximoPassoDisplay(ultimosRegistos, fasePlaneada),
                    ResumoFases = BuildResumoFases(ultimosRegistos, fases),
                    UltimosRegistosPorFase = new Dictionary<int, RegistoProducaoDto?>(ultimosRegistos)
                };
            });

            return await Task.WhenAll(itemTasks);
        });

        var porMolde = await Task.WhenAll(porMoldeTasks);

        return porMolde
            .SelectMany(items => items)
            .Where(item => item is not null)
            .Cast<ProducaoPecaDisponivelItem>()
            .ToList();
    }

    private async Task<Dictionary<int, RegistoProducaoDto?>> GetUltimosRegistosPorFaseAsync(
        int pecaId,
        IEnumerable<FaseProducaoItem> fases)
    {
        var tasks = fases.Select(async fase =>
        {
            var registo = await _registosProducaoService.GetUltimoAsync(fase.FasesProducao_id, pecaId);
            return new KeyValuePair<int, RegistoProducaoDto?>(fase.FasesProducao_id, registo);
        });

        var resultados = await Task.WhenAll(tasks);
        return resultados.ToDictionary(item => item.Key, item => item.Value);
    }

    private async Task<List<FilaGlobalMoldeItemDto>> GetAllFilaGlobalMoldeAsync()
    {
        var primeiraPagina = await _encomendasService.GetFilaGlobalMoldeAsync(1, 100)
            ?? throw new InvalidOperationException("Nao foi possivel carregar a fila global de moldes.");

        var moldes = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _encomendasService.GetFilaGlobalMoldeAsync(page, 100);
            if (pagina?.Items is null)
                continue;

            moldes.AddRange(pagina.Items);
        }

        return moldes;
    }

    private async Task<List<PecaDto>> GetAllPecasByMoldeIdAsync(int moldeId)
    {
        var primeiraPagina = await _pecasService.GetByMoldeIdAsync(moldeId, 1, 100)
            ?? throw new InvalidOperationException($"Nao foi possivel carregar as pecas do molde {moldeId}.");

        var pecas = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _pecasService.GetByMoldeIdAsync(moldeId, page, 100);
            if (pagina?.Items is null)
                continue;

            pecas.AddRange(pagina.Items);
        }

        return pecas;
    }

    private async Task<List<FaseProducaoItem>> GetAllFasesAsync()
    {
        var primeiraPagina = await _fasesProducaoService.GetAllAsync(1, 100)
            ?? throw new InvalidOperationException("Nao foi possivel carregar as fases de producao.");

        var fases = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _fasesProducaoService.GetAllAsync(page, 100);
            if (pagina?.Items is null)
                continue;

            fases.AddRange(pagina.Items);
        }

        return fases
            .OrderBy(item => GetPhaseSortOrder(item.Nome))
            .ThenBy(item => item.FasesProducao_id)
            .ToList();
    }

    private async Task<List<MaquinaItem>> GetAllMaquinasAsync()
    {
        var primeiraPagina = await _maquinasService.GetAllAsync(1, 100)
            ?? throw new InvalidOperationException("Nao foi possivel carregar as maquinas.");

        var maquinas = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _maquinasService.GetAllAsync(page, 100);
            if (pagina?.Items is null)
                continue;

            maquinas.AddRange(pagina.Items);
        }

        return maquinas;
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

    private void AplicarPaginacaoLista()
    {
        var pecasFiltradas = GetPecasFiltradas();

        var totalPages = pecasFiltradas.Count == 0
            ? 1
            : (int)Math.Ceiling((double)pecasFiltradas.Count / PageSize);

        UpdatePagination(pecasFiltradas.Count, totalPages);

        var itensPagina = pecasFiltradas
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        PecasDisponiveis.Clear();
        foreach (var item in itensPagina)
            PecasDisponiveis.Add(item);

        OnPropertyChanged(nameof(HasPecasDisponiveis));
        OnPropertyChanged(nameof(EmptyPecasMessage));
    }

    private List<ProducaoPecaDisponivelItem> GetPecasFiltradas()
    {
        IEnumerable<ProducaoPecaDisponivelItem> query = _todasPecasDisponiveis;

        if (string.IsNullOrWhiteSpace(SearchTerm))
            return query.ToList();

        var term = SearchTerm.Trim();

        query = SelectedSearchModeIndex switch
        {
            1 => query.Where(item =>
                item.DesignacaoDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.NumeroPecaDisplay.Contains(term, StringComparison.OrdinalIgnoreCase)),
            2 => query.Where(item =>
                item.ProximaFaseDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.FaseTrabalhoDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.ProximoPassoDisplay.Contains(term, StringComparison.OrdinalIgnoreCase)),
            _ => query.Where(item =>
                item.NumeroMoldeDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.NomeMoldeDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.NumeroEncomendaDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.NomeClienteDisplay.Contains(term, StringComparison.OrdinalIgnoreCase))
        };

        return query.ToList();
    }

    private void RestaurarSelecao(int? pecaId)
    {
        if (!pecaId.HasValue)
        {
            LimparSelecaoInterna();
            return;
        }

        SelectedPeca = _todasPecasDisponiveis.FirstOrDefault(item => item.PecaId == pecaId.Value);
    }

    private void LimparLista()
    {
        _todasPecasDisponiveis.Clear();
        PecasDisponiveis.Clear();
        UpdatePagination(0, 1);
        LimparSelecaoInterna();
        OnPropertyChanged(nameof(HasPecasDisponiveis));
        OnPropertyChanged(nameof(EmptyPecasMessage));
    }

    private void LimparSelecaoInterna()
    {
        SelectedPeca = null;
        SelectedFase = null;
        SelectedEstadoRegisto = null;
        SelectedMaquina = null;
        FasesRegistoDisponiveis.Clear();
        EstadosRegistoDisponiveis.Clear();
        MaquinasRegistoDisponiveis.Clear();
        OnPropertyChanged(nameof(IsMachineSelectionVisible));
        OnPropertyChanged(nameof(HasMaquinasRegistoDisponiveis));
        OnPropertyChanged(nameof(MaquinasHint));
    }

    private void AtualizarFasesRegistoDisponiveis()
    {
        FasesRegistoDisponiveis.Clear();
        EstadosRegistoDisponiveis.Clear();
        MaquinasRegistoDisponiveis.Clear();
        SelectedFase = null;
        SelectedEstadoRegisto = null;
        SelectedMaquina = null;

        if (SelectedPeca is null)
        {
            OnPropertyChanged(nameof(MaquinasHint));
            return;
        }

        var fasePlaneada = ResolveFasePlaneada(SelectedPeca.ProximaFaseId, _todasFases, SelectedPeca.UltimosRegistosPorFase);
        if (fasePlaneada is not null && GetEstadosDisponiveis(SelectedPeca, fasePlaneada).Count > 0)
            FasesRegistoDisponiveis.Add(fasePlaneada);

        SelectedFase = FasesRegistoDisponiveis.FirstOrDefault();
        OnPropertyChanged(nameof(MaquinasHint));
    }

    private void AtualizarEstadosRegistoDisponiveis()
    {
        EstadosRegistoDisponiveis.Clear();
        MaquinasRegistoDisponiveis.Clear();
        SelectedEstadoRegisto = null;
        SelectedMaquina = null;

        if (SelectedPeca is null || SelectedFase is null)
        {
            OnPropertyChanged(nameof(MaquinasHint));
            return;
        }

        foreach (var estado in GetEstadosDisponiveis(SelectedPeca, SelectedFase))
            EstadosRegistoDisponiveis.Add(estado);

        SelectedEstadoRegisto = EstadosRegistoDisponiveis.FirstOrDefault();
        OnPropertyChanged(nameof(MaquinasHint));
    }

    private void AtualizarMaquinasDisponiveis()
    {
        MaquinasRegistoDisponiveis.Clear();
        SelectedMaquina = null;

        if (SelectedFase is null || SelectedEstadoRegisto is null || !EstadoRequerMaquina(SelectedEstadoRegisto.Value))
        {
            OnPropertyChanged(nameof(HasMaquinasRegistoDisponiveis));
            OnPropertyChanged(nameof(MaquinasHint));
            return;
        }

        MaquinasRegistoDisponiveis.Add(CreateNenhumaMaquinaOption());

        foreach (var maquina in _todasMaquinas
                     .Where(item => item.FaseDedicada_id == SelectedFase.FasesProducao_id && item.Disponivel)
                     .OrderBy(item => item.Numero)
                     .ThenBy(item => item.NomeModeloDisplay))
        {
            MaquinasRegistoDisponiveis.Add(maquina);
        }

        SelectedMaquina = MaquinasRegistoDisponiveis.FirstOrDefault();
        OnPropertyChanged(nameof(HasMaquinasRegistoDisponiveis));
        OnPropertyChanged(nameof(MaquinasHint));
    }

    private static bool EstaASerTrabalhadaAgora(IReadOnlyDictionary<int, RegistoProducaoDto?> ultimosRegistos)
    {
        return ultimosRegistos.Values.Any(registo =>
            registo is not null &&
            (IsEstado(registo.EstadoProducao, "PREPARACAO") || IsEstado(registo.EstadoProducao, "EM_CURSO")));
    }

    private static bool EstadoContaComoAtivo(string? estado)
    {
        return IsEstado(estado, "PREPARACAO") || IsEstado(estado, "EM_CURSO");
    }

    private string GetProximoPassoDisplay(
        IReadOnlyDictionary<int, RegistoProducaoDto?> ultimosRegistos,
        FaseProducaoItem fase)
    {
        ultimosRegistos.TryGetValue(fase.FasesProducao_id, out var ultimo);
        var estadoAtual = Normalize(ultimo?.EstadoProducao);

        return estadoAtual switch
        {
            "PAUSADO" => $"Retomar {fase.NomeDisplay}",
            "PREPARACAO" => $"Continuar {fase.NomeDisplay}",
            "PENDENTE" => $"Iniciar {fase.NomeDisplay}",
            _ => $"Iniciar {fase.NomeDisplay}"
        };
    }

    private FaseProducaoItem? ResolveFasePlaneada(
        int? proximaFaseId,
        IEnumerable<FaseProducaoItem> fases,
        IReadOnlyDictionary<int, RegistoProducaoDto?> ultimosRegistos)
    {
        if (proximaFaseId.HasValue)
        {
            var faseConfigurada = fases.FirstOrDefault(item => item.FasesProducao_id == proximaFaseId.Value);
            if (faseConfigurada is not null)
                return faseConfigurada;
        }

        return GetFaseBloqueante(ultimosRegistos, fases);
    }

    private FaseProducaoItem? GetFaseBloqueante(
        IReadOnlyDictionary<int, RegistoProducaoDto?> ultimosRegistos,
        IEnumerable<FaseProducaoItem> fases)
    {
        foreach (var fase in fases.OrderBy(item => GetPhaseSortOrder(item.Nome)).ThenBy(item => item.FasesProducao_id))
        {
            ultimosRegistos.TryGetValue(fase.FasesProducao_id, out var ultimo);
            if (!IsEstado(ultimo?.EstadoProducao, "CONCLUIDO"))
                return fase;
        }

        return null;
    }

    private List<EstadoProducaoOption> GetEstadosDisponiveis(ProducaoPecaDisponivelItem item, FaseProducaoItem fase)
    {
        return GetEstadosDisponiveis(item.UltimosRegistosPorFase, fase);
    }

    private List<EstadoProducaoOption> GetEstadosDisponiveis(
        IReadOnlyDictionary<int, RegistoProducaoDto?> ultimosRegistos,
        FaseProducaoItem fase)
    {
        ultimosRegistos.TryGetValue(fase.FasesProducao_id, out var ultimo);
        var estadoAtual = Normalize(ultimo?.EstadoProducao);
        var isMontagem = IsFaseMontagem(fase.Nome);

        return estadoAtual switch
        {
            "" => [CreateEstadoOption(isMontagem ? "PENDENTE" : "PREPARACAO")],
            "PENDENTE" => isMontagem
                ? [CreateEstadoOption("EM_CURSO")]
                : [CreateEstadoOption("PREPARACAO")],
            "PREPARACAO" => [CreateEstadoOption("EM_CURSO")],
            "EM_CURSO" => [CreateEstadoOption("PAUSADO"), CreateEstadoOption("CONCLUIDO")],
            "PAUSADO" => isMontagem
                ? [CreateEstadoOption("EM_CURSO")]
                : [CreateEstadoOption("EM_CURSO"), CreateEstadoOption("PREPARACAO")],
            _ => []
        };
    }

    private static EstadoProducaoOption CreateEstadoOption(string value)
    {
        return new EstadoProducaoOption
        {
            Value = value,
            DisplayName = value.Replace('_', ' ')
        };
    }

    private static bool EstadoRequerMaquina(string estado)
    {
        return IsEstado(estado, "PREPARACAO") || IsEstado(estado, "EM_CURSO");
    }

    private static MaquinaItem CreateNenhumaMaquinaOption()
    {
        return new MaquinaItem
        {
            Maquina_id = 0,
            Numero = 0,
            NomeModelo = "Nenhuma Maquina",
            Estado = "DISPONIVEL"
        };
    }

    private static bool IsFaseMontagem(string nomeFase)
    {
        return string.Equals(Normalize(nomeFase), "MONTAGEM", StringComparison.Ordinal);
    }

    private static bool IsEstado(string? current, string expected)
    {
        return string.Equals(Normalize(current), expected, StringComparison.Ordinal);
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    private static int GetPhaseSortOrder(string? nome)
    {
        return Normalize(nome) switch
        {
            "MAQUINACAO" => 0,
            "EROSAO" => 1,
            "MONTAGEM" => 2,
            _ => 99
        };
    }

    private string GetNomeFaseDisplay(int faseId)
    {
        var fase = _todasFases.FirstOrDefault(item => item.FasesProducao_id == faseId);
        return fase?.NomeDisplay ?? $"Fase #{faseId}";
    }

    private string BuildResumoFases(
        IReadOnlyDictionary<int, RegistoProducaoDto?> ultimosRegistos,
        IEnumerable<FaseProducaoItem> fases)
    {
        var partes = fases.Select(fase =>
        {
            ultimosRegistos.TryGetValue(fase.FasesProducao_id, out var registo);
            var estado = registo?.EstadoProducao?.Replace('_', ' ') ?? "SEM HISTORICO";
            return $"{fase.NomeDisplay}: {estado}";
        });

        return string.Join(" | ", partes);
    }
}
