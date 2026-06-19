using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;

namespace TipMolde.ViewModel;

public partial class RegistoProducaoViewModel : ObservableObject
{
    private const string EstadoPendente = "PENDENTE";
    private const string EstadoPreparacao = "PREPARACAO";
    private const string EstadoEmCurso = "EM_CURSO";

    private readonly RegistosProducaoService _registosProducaoService;
    private readonly FasesProducaoService _fasesProducaoService;
    private readonly MaquinasService _maquinasService;
    private readonly PecasService _pecasService;
    private readonly MoldesService _moldesService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly UtilizadoresService _utilizadoresService;
    private readonly IDialogService _dialogService;

    private List<FaseProducaoItem> _todasFases = [];
    private List<MaquinaItem> _todasMaquinas = [];
    private List<RegistoProducaoDto> _todosRegistos = [];
    private int? _proximaFaseOriginalId;
    private bool _suppressProximaFaseChange;

    public RegistoProducaoViewModel(
        RegistoProducaoViewModelDependencies dependencies,
        SessaoPersistidaService sessaoPersistidaService,
        UtilizadoresService utilizadoresService,
        IDialogService dialogService)
    {
        _registosProducaoService = dependencies.RegistosProducaoService;
        _fasesProducaoService = dependencies.FasesProducaoService;
        _maquinasService = dependencies.MaquinasService;
        _pecasService = dependencies.PecasService;
        _moldesService = dependencies.MoldesService;
        _sessaoPersistidaService = sessaoPersistidaService;
        _utilizadoresService = utilizadoresService;
        _dialogService = dialogService;
    }

    public ObservableCollection<FaseProducaoItem> FasesDisponiveis { get; } = new();
    public ObservableCollection<FaseProducaoItem> ProximasFasesDisponiveis { get; } = new();
    public ObservableCollection<EstadoProducaoOption> EstadosDisponiveis { get; } = new();
    public ObservableCollection<RegistoProducaoMaquinaOption> MaquinasDisponiveis { get; } = new();
    public ObservableCollection<RegistoProducaoHistoricoItem> HistoricoRegistos { get; } = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private int? gestorProducaoId;

    [ObservableProperty]
    private string gestorProducaoNome = string.Empty;

    [ObservableProperty]
    private ProducaoPecaDisponivelItem? pecaContexto;

    [ObservableProperty]
    private RegistoProducaoDto? registoAtivoAtual;

    [ObservableProperty]
    private FaseProducaoItem? selectedFase;

    [ObservableProperty]
    private EstadoProducaoOption? selectedEstado;

    [ObservableProperty]
    private RegistoProducaoMaquinaOption? selectedMaquina;

    [ObservableProperty]
    private FaseProducaoItem? selectedProximaFase;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasPeca => PecaContexto is not null;
    public bool HasRegistoAtivo => RegistoAtivoAtual is not null && EstadoContaComoAtivo(RegistoAtivoAtual.EstadoProducao);
    public bool CanSelecionarFase => !HasRegistoAtivo;
    public bool CanEditarProximaFase => !HasRegistoAtivo && PecaContexto is not null && ProximasFasesDisponiveis.Count > 0;
    public bool IsMachineSelectionVisible => SelectedEstado is not null && EstadoRequerMaquina(SelectedEstado.Value);
    public bool CanSelecionarMaquina => IsMachineSelectionVisible && !DeveManterMaquinaDaPreparacao();
    public string GestorProducaoDisplay => GetGestorProducaoDisplay();
    public string NumeroMoldeDisplay => PecaContexto?.NumeroMoldeDisplay ?? "Molde por carregar";
    public string NomeMoldeDisplay => PecaContexto?.NomeMoldeDisplay ?? "Molde por carregar";
    public string DesignacaoPecaDisplay => PecaContexto?.DesignacaoDisplay ?? "Peca por carregar";
    public string NumeroPecaDisplay => PecaContexto?.NumeroPecaDisplay ?? "Sem numero";
    public string PrioridadeDisplay => PecaContexto?.PrioridadeResumo ?? "Prioridade indisponivel";
    public string DataEntregaPrevistaDisplay => PecaContexto?.EntregaDisplay ?? "Sem data";
    public string EstadoAtualDisplay => GetRegistoAtualDisplay();
    public string FaseAtualDisplay => GetFaseAtualDisplay();
    public string ProximaFasePlaneadaDisplay => SelectedProximaFase?.NomeDisplay ?? PecaContexto?.ProximaFaseDisplay ?? "Sem fase planeada";
    public string PageTitle => HasRegistoAtivo ? "Producao em Curso" : "Novo Registo de Producao";
    public string IntroText => HasRegistoAtivo
        ? "Ha uma producao ativa nesta peca. Podes continuar o registo e consultar o historico abaixo."
        : "Escolhe a fase, o estado e a proxima fase com seguranca antes de guardar.";
    public string SaveButtonText => IsSaving ? "A guardar..." : "Guardar registo";
    public string MachineHint => BuildMachineHint();
    public string ProximaFaseHint => BuildProximaFaseHint();
    public string HistoricoTituloDisplay => HasHistorico
        ? $"Historico da peca ({HistoricoRegistos.Count})"
        : "Historico da peca";
    public string HistoricoCountDisplay => HasHistorico
        ? $"{HistoricoRegistos.Count} registos"
        : "Sem registos";
    public string EmptyHistoricoMessage => "Ainda nao existem registos de producao para esta peca.";
    public bool HasHistorico => HistoricoRegistos.Count > 0;
    public bool CanGuardar =>
        !IsLoading &&
        !IsSaving &&
        GestorProducaoId.HasValue &&
        PecaContexto is not null &&
        SelectedFase is not null &&
        SelectedEstado is not null;

    partial void OnSelectedFaseChanged(FaseProducaoItem? value)
    {
        OnPropertyChanged(nameof(FaseAtualDisplay));
        AtualizarEstados();
        OnPropertyChanged(nameof(CanSelecionarMaquina));
        OnPropertyChanged(nameof(MachineHint));
        GuardarCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedEstadoChanged(EstadoProducaoOption? value)
    {
        OnPropertyChanged(nameof(IsMachineSelectionVisible));
        OnPropertyChanged(nameof(CanSelecionarMaquina));
        AtualizarMaquinas();
        OnPropertyChanged(nameof(MachineHint));
        GuardarCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedMaquinaChanged(RegistoProducaoMaquinaOption? value)
    {
        GuardarCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedProximaFaseChanged(FaseProducaoItem? value)
    {
        OnPropertyChanged(nameof(ProximaFasePlaneadaDisplay));
        OnPropertyChanged(nameof(ProximaFaseHint));

        if (_suppressProximaFaseChange)
            return;

        _ = ConfirmarAlteracaoProximaFaseAsync(value);
    }

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    partial void OnGestorProducaoIdChanged(int? value)
    {
        OnPropertyChanged(nameof(GestorProducaoDisplay));
        GuardarCommand.NotifyCanExecuteChanged();
    }

    partial void OnGestorProducaoNomeChanged(string value) => OnPropertyChanged(nameof(GestorProducaoDisplay));

    private string GetGestorProducaoDisplay()
    {
        if (!GestorProducaoId.HasValue)
            return "Sessao sem gestor de producao identificado";

        var gestorNome = string.IsNullOrWhiteSpace(GestorProducaoNome)
            ? $"Gestor de producao #{GestorProducaoId}"
            : GestorProducaoNome;

        return $"{gestorNome} (#{GestorProducaoId})";
    }

    partial void OnPecaContextoChanged(ProducaoPecaDisponivelItem? value)
    {
        OnPropertyChanged(nameof(HasPeca));
        OnPropertyChanged(nameof(NumeroMoldeDisplay));
        OnPropertyChanged(nameof(NomeMoldeDisplay));
        OnPropertyChanged(nameof(DesignacaoPecaDisplay));
        OnPropertyChanged(nameof(NumeroPecaDisplay));
        OnPropertyChanged(nameof(PrioridadeDisplay));
        OnPropertyChanged(nameof(DataEntregaPrevistaDisplay));
        OnPropertyChanged(nameof(EstadoAtualDisplay));
        OnPropertyChanged(nameof(FaseAtualDisplay));
        OnPropertyChanged(nameof(ProximaFasePlaneadaDisplay));
        OnPropertyChanged(nameof(CanEditarProximaFase));
        OnPropertyChanged(nameof(ProximaFaseHint));
        OnPropertyChanged(nameof(HistoricoTituloDisplay));
        OnPropertyChanged(nameof(HasHistorico));
        GuardarCommand.NotifyCanExecuteChanged();
    }

    partial void OnRegistoAtivoAtualChanged(RegistoProducaoDto? value)
    {
        OnPropertyChanged(nameof(HasRegistoAtivo));
        OnPropertyChanged(nameof(CanSelecionarFase));
        OnPropertyChanged(nameof(CanEditarProximaFase));
        OnPropertyChanged(nameof(ProximaFaseHint));
        OnPropertyChanged(nameof(EstadoAtualDisplay));
        OnPropertyChanged(nameof(FaseAtualDisplay));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(IntroText));
        OnPropertyChanged(nameof(HistoricoTituloDisplay));
        OnPropertyChanged(nameof(HistoricoCountDisplay));
    }

    public async Task LoadAsync(ProducaoPecaDisponivelItem? requestedContext)
    {
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            var gestorProducaoTask = GetGestorProducaoAtualAsync();
            var fasesTask = GetAllFasesAsync();
            var maquinasTask = GetAllMaquinasAsync();
            var registosTask = GetAllRegistosAsync();

            await Task.WhenAll(gestorProducaoTask, fasesTask, maquinasTask, registosTask);

            GestorProducaoId = gestorProducaoTask.Result?.User_id;
            GestorProducaoNome = gestorProducaoTask.Result?.Nome ?? string.Empty;
            _todasFases = fasesTask.Result;
            _todasMaquinas = maquinasTask.Result;
            _todosRegistos = registosTask.Result;

            if (!GestorProducaoId.HasValue)
                throw new InvalidOperationException("Nao foi possivel identificar o gestor de producao autenticado.");

            var registoAtivo = GetRegistoAtivoDoGestorProducao(GestorProducaoId.Value, _todosRegistos);
            RegistoAtivoAtual = registoAtivo;

            if (registoAtivo is not null)
            {
                var activeContext = await BuildContextFromPecaIdAsync(registoAtivo.PecaId, requestedContext);
                PecaContexto = activeContext;

                if (requestedContext is not null && requestedContext.PecaId != registoAtivo.PecaId)
                {
                    await _dialogService.ShowInfoAsync(
                        "Registo ativo",
                        "Ja tens um registo em curso. O sistema abriu diretamente a tua peca ativa.");
                }
            }
            else if (requestedContext is not null)
            {
                PecaContexto = requestedContext;
            }
            else
            {
                throw new InvalidOperationException("Nao existe registo ativo para apresentar.");
            }

            AtualizarFases();
            AtualizarHistorico();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            GuardarCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private static async Task VoltarAsync()
    {
        await ShellNavigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task AbrirRegistoAtivoAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            if (!GestorProducaoId.HasValue)
            {
                await _dialogService.ShowInfoAsync("Registo ativo", "Nao foi possivel identificar o gestor de producao autenticado.");
                return;
            }

            var registoAtivo = GetRegistoAtivoDoGestorProducao(GestorProducaoId.Value, _todosRegistos);
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

    [RelayCommand(CanExecute = nameof(CanGuardar))]
    private async Task GuardarAsync()
    {
        if (!CanGuardar || PecaContexto is null || SelectedFase is null || SelectedEstado is null || !GestorProducaoId.HasValue)
            return;

        IsSaving = true;
        ErrorMessage = string.Empty;

        try
        {
            await _registosProducaoService.CreateAsync(
                PecaContexto.PecaId,
                SelectedFase.FasesProducao_id,
                GestorProducaoId.Value,
                SelectedEstado.Value,
                SelectedMaquina?.MaquinaId,
                SelectedProximaFase?.FasesProducao_id);

            await _dialogService.ShowSuccessAsync(
                "Registo guardado",
                $"Foi registado o estado {SelectedEstado.DisplayName} para a peca {PecaContexto.DesignacaoDisplay}.");

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
            GuardarCommand.NotifyCanExecuteChanged();
        }
    }

    private void AtualizarFases()
    {
        _suppressProximaFaseChange = true;
        try
        {
            FasesDisponiveis.Clear();
            ProximasFasesDisponiveis.Clear();
            EstadosDisponiveis.Clear();
            MaquinasDisponiveis.Clear();
            SelectedFase = null;
            SelectedEstado = null;
            SelectedMaquina = null;
            SelectedProximaFase = null;

            if (PecaContexto is null)
                return;

            if (HasRegistoAtivo && RegistoAtivoAtual is not null)
            {
                var faseAtiva = _todasFases.First(item => item.FasesProducao_id == RegistoAtivoAtual.FaseId);
                if (faseAtiva is not null)
                {
                    FasesDisponiveis.Add(faseAtiva);
                    SelectedFase = faseAtiva;
                }

                PreencherProximasFasesDisponiveis();
                _proximaFaseOriginalId = SelectedProximaFase?.FasesProducao_id;
                return;
            }

            var fasePlaneada = ResolveFasePlaneada(PecaContexto.UltimosRegistosPorFase, PecaContexto.ProximaFaseId);
            if (fasePlaneada is null)
                return;

            if (GetEstadosDisponiveis(PecaContexto, fasePlaneada).Count == 0)
                return;

            FasesDisponiveis.Add(fasePlaneada);
            SelectedFase = fasePlaneada;
            PreencherProximasFasesDisponiveis();
            _proximaFaseOriginalId = SelectedProximaFase?.FasesProducao_id;
        }
        finally
        {
            _suppressProximaFaseChange = false;
            OnPropertyChanged(nameof(CanEditarProximaFase));
            OnPropertyChanged(nameof(ProximaFaseHint));
        }
    }

    private void AtualizarEstados()
    {
        EstadosDisponiveis.Clear();
        MaquinasDisponiveis.Clear();
        SelectedEstado = null;
        SelectedMaquina = null;

        if (PecaContexto is null || SelectedFase is null)
            return;

        foreach (var estado in GetEstadosDisponiveis(PecaContexto, SelectedFase))
            EstadosDisponiveis.Add(estado);

        if (EstadosDisponiveis.Count > 0)
            SelectedEstado = EstadosDisponiveis.First();
    }

    private void AtualizarMaquinas()
    {
        MaquinasDisponiveis.Clear();
        SelectedMaquina = null;

        if (SelectedFase is null || SelectedEstado is null || !EstadoRequerMaquina(SelectedEstado.Value))
        {
            OnPropertyChanged(nameof(CanSelecionarMaquina));
            OnPropertyChanged(nameof(MachineHint));
            return;
        }

        if (DeveManterMaquinaDaPreparacao())
        {
            var ultimoRegisto = GetUltimoRegistoFaseSelecionada();
            if (ultimoRegisto?.MaquinaId is int maquinaId)
            {
                var maquinaAnterior = _todasMaquinas.First(item => item.Maquina_id == maquinaId);
                MaquinasDisponiveis.Add(new RegistoProducaoMaquinaOption
                {
                    MaquinaId = maquinaId,
                    DisplayName = maquinaAnterior?.DisplayName ?? $"Maquina #{maquinaId}",
                    Maquina = maquinaAnterior
                });
            }
            else
            {
                MaquinasDisponiveis.Add(CreateNenhumaMaquinaOption());
            }

            if (MaquinasDisponiveis.Count > 0)
                SelectedMaquina = MaquinasDisponiveis.First();
            OnPropertyChanged(nameof(CanSelecionarMaquina));
            OnPropertyChanged(nameof(MachineHint));
            return;
        }

        MaquinasDisponiveis.Add(CreateNenhumaMaquinaOption());

        foreach (var maquina in _todasMaquinas
                     .Where(item => item.FaseDedicada_id == SelectedFase.FasesProducao_id && item.Disponivel)
                     .OrderBy(item => item.Numero)
                     .ThenBy(item => item.NomeModeloDisplay))
        {
            MaquinasDisponiveis.Add(new RegistoProducaoMaquinaOption
            {
                MaquinaId = maquina.Maquina_id,
                DisplayName = maquina.DisplayName,
                Maquina = maquina
            });
        }

        if (MaquinasDisponiveis.Count > 0)
            SelectedMaquina = MaquinasDisponiveis.First();
        OnPropertyChanged(nameof(CanSelecionarMaquina));
        OnPropertyChanged(nameof(MachineHint));
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

    private static RegistoProducaoDto? GetRegistoAtivoDoGestorProducao(
        int gestorProducaoId,
        IEnumerable<RegistoProducaoDto> registos)
    {
        return registos
            .GroupBy(item => new { item.PecaId, item.FaseId })
            .Select(group => group.OrderByDescending(item => item.DataHora).First())
            .Where(item => item.GestorProducaoId == gestorProducaoId && EstadoContaComoAtivo(item.EstadoProducao))
            .OrderByDescending(item => item.DataHora)
            .FirstOrDefault();
    }

    private async Task<ProducaoPecaDisponivelItem> BuildContextFromPecaIdAsync(int pecaId, ProducaoPecaDisponivelItem? fallback)
    {
        var peca = await _pecasService.GetByIdAsync(pecaId)
            ?? throw new InvalidOperationException($"Nao foi possivel carregar a peca {pecaId}.");

        var molde = await _moldesService.GetByIdAsync(peca.Molde_id);
        var ultimosPorFase = await GetUltimosRegistosPorFaseAsync(pecaId, _todasFases);
        var ultimoGlobal = GetUltimoRegistoGlobal(ultimosPorFase);

        return new ProducaoPecaDisponivelItem
        {
            MoldeId = peca.Molde_id,
            PecaId = peca.PecaId,
            PrioridadeMolde = fallback?.PrioridadeMolde ?? 0,
            PrioridadePeca = peca.Prioridade,
            Quantidade = peca.Quantidade,
            NumeroMolde = fallback?.NumeroMolde ?? molde?.Numero ?? string.Empty,
            NomeMolde = fallback?.NomeMolde ?? molde?.Nome ?? string.Empty,
            NumeroEncomendaCliente = fallback?.NumeroEncomendaCliente ?? string.Empty,
            NomeCliente = fallback?.NomeCliente ?? string.Empty,
            Designacao = peca.Designacao,
            NumeroPeca = peca.NumeroPeca,
            DataEntregaPrevista = fallback?.DataEntregaPrevista ?? default,
            UltimoEstadoGlobal = ultimoGlobal?.EstadoProducao ?? string.Empty,
            UltimaFaseGlobal = ultimoGlobal is null ? string.Empty : GetNomeFaseDisplay(ultimoGlobal.FaseId),
            ProximaFaseId = peca.ProximaFase_id,
            ProximaFaseNome = string.IsNullOrWhiteSpace(peca.ProximaFaseNome) ? string.Empty : peca.ProximaFaseNome.Replace('_', ' '),
            ResumoFases = BuildResumoFases(ultimosPorFase, _todasFases),
            UltimosRegistosPorFase = ultimosPorFase
        };
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

    private List<EstadoProducaoOption> GetEstadosDisponiveis(ProducaoPecaDisponivelItem item, FaseProducaoItem fase)
    {
        return GetEstadosDisponiveis(item.UltimosRegistosPorFase, fase);
    }

    private List<EstadoProducaoOption> GetEstadosDisponiveis(
        Dictionary<int, RegistoProducaoDto?> ultimosRegistos,
        FaseProducaoItem fase)
    {
        var faseBloqueante = ResolveFasePlaneada(ultimosRegistos, PecaContexto?.ProximaFaseId);
        if (faseBloqueante is null || faseBloqueante.FasesProducao_id != fase.FasesProducao_id)
            return [];

        ultimosRegistos.TryGetValue(fase.FasesProducao_id, out var ultimo);
        var estadoAtual = Normalize(ultimo?.EstadoProducao);
        var isMontagem = IsFaseMontagem(fase.Nome);

        return estadoAtual switch
        {
            "" => [CreateEstadoOption(isMontagem ? EstadoPendente : EstadoPreparacao)],
            "PENDENTE" => isMontagem
                ? [CreateEstadoOption(EstadoEmCurso)]
                : [CreateEstadoOption(EstadoPreparacao)],
            EstadoPreparacao => [CreateEstadoOption(EstadoEmCurso), CreateEstadoOption("PAUSADO")],
            EstadoEmCurso => [CreateEstadoOption("PAUSADO"), CreateEstadoOption("CONCLUIDO")],
            "PAUSADO" => isMontagem
                ? [CreateEstadoOption(EstadoEmCurso)]
                : [CreateEstadoOption(EstadoPreparacao), CreateEstadoOption(EstadoEmCurso)],
            _ => []
        };
    }

    private static FaseProducaoItem? GetFaseBloqueante(
        Dictionary<int, RegistoProducaoDto?> ultimosRegistos,
        IEnumerable<FaseProducaoItem> fases)
    {
        foreach (var fase in fases)
        {
            ultimosRegistos.TryGetValue(fase.FasesProducao_id, out var ultimo);
            if (!IsEstado(ultimo?.EstadoProducao, "CONCLUIDO"))
                return fase;
        }

        return null;
    }

    private FaseProducaoItem? ResolveFasePlaneada(
        Dictionary<int, RegistoProducaoDto?> ultimosRegistos,
        int? proximaFaseId)
    {
        if (proximaFaseId.HasValue)
        {
            var faseConfigurada = _todasFases.First(item => item.FasesProducao_id == proximaFaseId.Value);
            if (faseConfigurada is not null)
                return faseConfigurada;
        }

        return GetFaseBloqueante(ultimosRegistos, _todasFases);
    }

    private RegistoProducaoDto? GetUltimoRegistoFaseSelecionada()
    {
        if (PecaContexto is null || SelectedFase is null)
            return null;

        PecaContexto.UltimosRegistosPorFase.TryGetValue(SelectedFase.FasesProducao_id, out var ultimo);
        return ultimo;
    }

    private RegistoProducaoDto? GetRegistoAtual()
    {
        return RegistoAtivoAtual ?? GetUltimoRegistoGlobal(PecaContexto?.UltimosRegistosPorFase);
    }

    private static RegistoProducaoDto? GetUltimoRegistoGlobal(IReadOnlyDictionary<int, RegistoProducaoDto?>? ultimosRegistos)
    {
        return ultimosRegistos?
            .Values
            .Where(item => item is not null)
            .OrderByDescending(item => item!.DataHora)
            .FirstOrDefault();
    }

    private bool DeveManterMaquinaDaPreparacao()
    {
        return SelectedEstado is not null &&
               IsEstado(SelectedEstado.Value, EstadoEmCurso) &&
               IsEstado(GetUltimoRegistoFaseSelecionada()?.EstadoProducao, EstadoPreparacao);
    }

    private string GetRegistoAtualDisplay()
    {
        var registoAtual = GetRegistoAtual();
        return registoAtual is null
            ? "Sem registo."
            : registoAtual.EstadoProducao.Replace('_', ' ');
    }

    private string GetFaseAtualDisplay()
    {
        var registoAtual = GetRegistoAtual();
        return registoAtual is null
            ? "Sem fase atual"
            : GetNomeFaseDisplay(registoAtual.FaseId);
    }

    private string GetNomeFaseDisplay(int faseId)
    {
        var fase = _todasFases.FirstOrDefault(item => item.FasesProducao_id == faseId);
        return fase?.NomeDisplay ?? $"Fase #{faseId}";
    }

    private string BuildMachineHint()
    {
        if (!IsMachineSelectionVisible)
            return "Esta transicao nao exige maquina.";

        if (DeveManterMaquinaDaPreparacao())
            return $"A transicao de {EstadoPreparacao} para {EstadoEmCurso} tem de manter a mesma maquina da preparacao.";

        if (MaquinasDisponiveis.Count == 0)
            return "Nao ha opcoes de maquina disponiveis para esta fase, mas podes continuar sem maquina.";

        return "Podes escolher uma maquina dedicada ou Nenhuma Maquina para trabalho manual.";
    }

    private static RegistoProducaoMaquinaOption CreateNenhumaMaquinaOption()
    {
        return new RegistoProducaoMaquinaOption
        {
            MaquinaId = null,
            DisplayName = "Nenhuma Maquina"
        };
    }

    private static string BuildResumoFases(
        Dictionary<int, RegistoProducaoDto?> ultimosRegistos,
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

    private static EstadoProducaoOption CreateEstadoOption(string value)
    {
        return new EstadoProducaoOption
        {
            Value = value,
            DisplayName = value.Replace('_', ' ')
        };
    }

    private static bool EstadoContaComoAtivo(string? estado)
    {
        return IsEstado(estado, EstadoPreparacao) || IsEstado(estado, EstadoEmCurso);
    }

    private static bool EstadoRequerMaquina(string estado)
    {
        return IsEstado(estado, EstadoPreparacao) || IsEstado(estado, EstadoEmCurso);
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

    private void PreencherProximasFasesDisponiveis()
    {
        ProximasFasesDisponiveis.Clear();

        foreach (var fase in _todasFases)
            ProximasFasesDisponiveis.Add(fase);

        SelectedProximaFase = ProximasFasesDisponiveis.FirstOrDefault(item => item.FasesProducao_id == PecaContexto?.ProximaFaseId)
            ?? SelectedFase
            ?? ProximasFasesDisponiveis.FirstOrDefault();
    }

    private async Task ConfirmarAlteracaoProximaFaseAsync(FaseProducaoItem? novaFase)
    {
        try
        {
            if (PecaContexto is null || novaFase is null)
                return;

            if (HasRegistoAtivo)
            {
                RestaurarProximaFaseOriginal();
                return;
            }

            if (!_proximaFaseOriginalId.HasValue || novaFase.FasesProducao_id == _proximaFaseOriginalId.Value)
                return;

            var faseOriginal = _todasFases.FirstOrDefault(item => item.FasesProducao_id == _proximaFaseOriginalId.Value);
            var originalDisplay = faseOriginal?.NomeDisplay ?? "fase atual";
            var novoDisplay = novaFase.NomeDisplay;

            var confirmar = await _dialogService.ShowSelectionAsync(
                $"Alterar a proxima fase de {originalDisplay} para {novoDisplay}?",
                "Cancelar",
                "Confirmar");

            if (confirmar is null)
            {
                RestaurarProximaFaseOriginal();
                return;
            }

            await _pecasService.UpdateProximaFaseAsync(PecaContexto.PecaId, novaFase.FasesProducao_id);

            await _dialogService.ShowSuccessAsync(
                "Proxima fase atualizada",
                $"A proxima fase da peca {DesignacaoPecaDisplay} foi atualizada para {novoDisplay}.");

            await RecarregarContextoAtualAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Proxima fase", ex.Message);
            RestaurarProximaFaseOriginal();
        }
    }

    private void RestaurarProximaFaseOriginal()
    {
        _suppressProximaFaseChange = true;

        try
        {
            SelectedProximaFase = _todasFases.FirstOrDefault(item => item.FasesProducao_id == _proximaFaseOriginalId)
                ?? ProximasFasesDisponiveis.FirstOrDefault();
        }
        finally
        {
            _suppressProximaFaseChange = false;
        }
    }

    private void AtualizarHistorico()
    {
        HistoricoRegistos.Clear();

        if (PecaContexto is null || _todosRegistos.Count == 0)
        {
            OnPropertyChanged(nameof(HasHistorico));
            OnPropertyChanged(nameof(HistoricoTituloDisplay));
            OnPropertyChanged(nameof(HistoricoCountDisplay));
            return;
        }

        foreach (var registo in _todosRegistos
                     .Where(item => item.PecaId == PecaContexto.PecaId)
                     .OrderByDescending(item => item.DataHora))
        {
            HistoricoRegistos.Add(CreateHistoricoItem(registo));
        }

        OnPropertyChanged(nameof(HasHistorico));
        OnPropertyChanged(nameof(HistoricoTituloDisplay));
        OnPropertyChanged(nameof(HistoricoCountDisplay));
    }

    private async Task RecarregarContextoAtualAsync()
    {
        if (PecaContexto is null)
            return;

        var pecaAtualizada = await BuildContextFromPecaIdAsync(PecaContexto.PecaId, PecaContexto);
        PecaContexto = pecaAtualizada;

        AtualizarFases();
        AtualizarHistorico();
    }

    private RegistoProducaoHistoricoItem CreateHistoricoItem(RegistoProducaoDto registo)
    {
        var fase = _todasFases.FirstOrDefault(item => item.FasesProducao_id == registo.FaseId);

        return new RegistoProducaoHistoricoItem
        {
            RegistoProducaoId = registo.RegistoProducaoId,
            FaseId = registo.FaseId,
            FaseDisplay = fase?.NomeDisplay ?? $"Fase #{registo.FaseId}",
            EstadoProducao = registo.EstadoProducao,
            DataHora = registo.DataHora,
            GestorProducaoId = registo.GestorProducaoId,
            MaquinaId = registo.MaquinaId
        };
    }

    private string BuildProximaFaseHint()
    {
        if (PecaContexto is null || ProximasFasesDisponiveis.Count == 0)
            return "Sem fases disponiveis para planeamento.";

        if (HasRegistoAtivo)
            return "A proxima fase fica bloqueada enquanto existir uma producao ativa.";

        return "Podes alterar a proxima fase antes de guardar. Se mudares para outra fase, vamos pedir confirmacao.";
    }
}
