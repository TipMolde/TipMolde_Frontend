using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

/// <summary>
/// Apresenta o detalhe operacional de uma maquina e permite completar contexto industrial pendente.
/// </summary>
public partial class MaquinaDetalheViewModel : PaginatedViewModel
{
    private const int PecasPageSize = 5;
    private readonly FasesProducaoService _fasesProducaoService;
    private readonly MaquinasService _maquinasService;
    private readonly IndustrialProducaoService _industrialProducaoService;
    private readonly PecasService _pecasService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly UtilizadoresService _utilizadoresService;
    private readonly IDialogService _dialogService;
    private List<FaseProducaoItem> _todasFases = [];

    /// <summary>
    /// Construtor do view model de detalhe de maquina.
    /// </summary>
    /// <param name="maquinasService">Servico usado para carregar os dados base da maquina.</param>
    /// <param name="industrialProducaoService">Servico usado para consultar e completar eventos industriais.</param>
    /// <param name="pecasService">Servico usado para pesquisar pecas elegiveis para associacao.</param>
    /// <param name="sessaoPersistidaService">Servico usado para identificar a sessao atual.</param>
    /// <param name="utilizadoresService">Servico usado para resolver o utilizador autenticado.</param>
    /// <param name="dialogService">Servico usado para apresentar mensagens de sucesso ou erro.</param>
    public MaquinaDetalheViewModel(
        FasesProducaoService fasesProducaoService,
        MaquinasService maquinasService,
        IndustrialProducaoService industrialProducaoService,
        PecasService pecasService,
        SessaoPersistidaService sessaoPersistidaService,
        UtilizadoresService utilizadoresService,
        IDialogService dialogService)
    {
        _fasesProducaoService = fasesProducaoService;
        _maquinasService = maquinasService;
        _industrialProducaoService = industrialProducaoService;
        _pecasService = pecasService;
        _sessaoPersistidaService = sessaoPersistidaService;
        _utilizadoresService = utilizadoresService;
        _dialogService = dialogService;
        PageSize = PecasPageSize;

        PropertyChanged += (_, args) =>
        {
            if (!string.Equals(args.PropertyName, nameof(IsLoading), StringComparison.Ordinal))
                return;

            OnPropertyChanged(nameof(CanCompletarContexto));
            OnPropertyChanged(nameof(CanIniciarConclusao));
            OnPropertyChanged(nameof(CanConfirmarParagem));
            OnPropertyChanged(nameof(CanConfirmarConclusao));
            CompletarContextoCommand.NotifyCanExecuteChanged();
            ConfirmarPausaCommand.NotifyCanExecuteChanged();
            PrepararConclusaoCommand.NotifyCanExecuteChanged();
            CancelarConclusaoCommand.NotifyCanExecuteChanged();
            ConfirmarConclusaoCommand.NotifyCanExecuteChanged();
        };
    }

    public ObservableCollection<ProducaoPecaDisponivelItem> PecasEncontradas { get; } = new();
    public ObservableCollection<FaseProducaoItem> ProximasFasesDisponiveis { get; } = new();

    [ObservableProperty]
    private MaquinaItem? maquina;

    [ObservableProperty]
    private IndustrialEventoDto? eventoPendente;

    [ObservableProperty]
    private IndustrialSessaoAtivaDto? sessaoAtiva;

    [ObservableProperty]
    private ProducaoPecaDisponivelItem? selectedPeca;

    [ObservableProperty]
    private FaseProducaoItem? selectedProximaFase;

    [ObservableProperty]
    private int? utilizadorAtualId;

    [ObservableProperty]
    private string utilizadorAtualNome = string.Empty;

    [ObservableProperty]
    private string pesquisaPeca = string.Empty;

    [ObservableProperty]
    private bool isSearchingPecas;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private bool isConclusaoSelectionActive;

    [ObservableProperty]
    private string infoMessage = string.Empty;

    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool HasMaquina => Maquina is not null;
    public bool HasEventoPendente => EventoPendente is not null;
    public bool HasEventoRunningPendente => IsEventoRunning(EventoPendente);
    public bool HasEventoStoppedPendente => IsEventoStopped(EventoPendente);
    public bool NeedsContextSelection => HasEventoRunningPendente && !HasSessaoAtiva;
    public bool HasAcaoPendente => NeedsContextSelection || HasEventoStoppedPendente;
    public bool HasNoAcaoPendente => !HasAcaoPendente;
    public bool HasSessaoAtiva => SessaoAtiva is not null;
    public bool HasPecasEncontradas => PecasEncontradas.Count > 0;
    public bool HasSelectedPeca => SelectedPeca is not null;
    public bool CanIniciarConclusao => CanConfirmarParagem && !IsConclusaoSelectionActive;
    public bool CanEscolherProximaFase => IsConclusaoSelectionActive && HasEventoStoppedPendente && ProximasFasesDisponiveis.Count > 0;
    public bool CanCompletarContexto => !IsLoading
                                        && !IsSaving
                                        && Maquina is not null
                                        && NeedsContextSelection
                                        && SelectedPeca is not null
                                        && UtilizadorAtualId.HasValue;
    public bool CanConfirmarParagem => !IsLoading
                                       && !IsSaving
                                       && HasEventoStoppedPendente;
    public bool CanConfirmarConclusao => IsConclusaoSelectionActive && CanConfirmarParagem && SelectedProximaFase is not null;

    public string MaquinaTitulo => Maquina is null
        ? "Maquina"
        : $"{Maquina.NumeroDisplay} - {Maquina.NomeModeloDisplay}";

    public string UtilizadorAtualDisplay => UtilizadorAtualId.HasValue
        ? $"{UtilizadorAtualNome} ({UtilizadorAtualId.Value})"
        : "Utilizador atual indisponivel";

    public string EventoPendenteDisplay => EventoPendente is null
        ? "Sem evento pendente para esta maquina."
        : $"{EventoPendente.EstadoMaquinaDisplay} recebido em {EventoPendente.OccurredAtDisplay}";

    public string SelectedPecaDisplay => SelectedPeca is null
        ? "Nenhuma peca selecionada"
        : $"{SelectedPeca.NumeroPecaDisplay} - {SelectedPeca.DesignacaoDisplay}";

    public string SessaoAtivaDisplay => SessaoAtiva is null
        ? "Sem peca ativa."
        : $"{SessaoAtiva.PecaResumoDisplay} em {SessaoAtiva.FaseDisplay}";

    public string ProximaFasePlaneadaDisplay => SelectedProximaFase?.NomeDisplay
        ?? SessaoAtiva?.ProximaFasePlaneadaDisplay
        ?? "Sem fase planeada";

    partial void OnMaquinaChanged(MaquinaItem? value)
    {
        OnPropertyChanged(nameof(HasMaquina));
        OnPropertyChanged(nameof(MaquinaTitulo));
        OnPropertyChanged(nameof(CanCompletarContexto));
        CompletarContextoCommand.NotifyCanExecuteChanged();
    }

    partial void OnEventoPendenteChanged(IndustrialEventoDto? value)
    {
        OnPropertyChanged(nameof(HasEventoPendente));
        OnPropertyChanged(nameof(HasEventoRunningPendente));
        OnPropertyChanged(nameof(HasEventoStoppedPendente));
        OnPropertyChanged(nameof(NeedsContextSelection));
        OnPropertyChanged(nameof(HasAcaoPendente));
        OnPropertyChanged(nameof(HasNoAcaoPendente));
        OnPropertyChanged(nameof(EventoPendenteDisplay));
        OnPropertyChanged(nameof(CanIniciarConclusao));
        OnPropertyChanged(nameof(CanEscolherProximaFase));
        OnPropertyChanged(nameof(CanCompletarContexto));
        OnPropertyChanged(nameof(CanConfirmarParagem));
        OnPropertyChanged(nameof(CanConfirmarConclusao));
        CompletarContextoCommand.NotifyCanExecuteChanged();
        ConfirmarPausaCommand.NotifyCanExecuteChanged();
        PrepararConclusaoCommand.NotifyCanExecuteChanged();
        CancelarConclusaoCommand.NotifyCanExecuteChanged();
        ConfirmarConclusaoCommand.NotifyCanExecuteChanged();
    }

    partial void OnSessaoAtivaChanged(IndustrialSessaoAtivaDto? value)
    {
        OnPropertyChanged(nameof(HasSessaoAtiva));
        OnPropertyChanged(nameof(SessaoAtivaDisplay));
        OnPropertyChanged(nameof(NeedsContextSelection));
        OnPropertyChanged(nameof(HasAcaoPendente));
        OnPropertyChanged(nameof(HasNoAcaoPendente));
        OnPropertyChanged(nameof(ProximaFasePlaneadaDisplay));
        OnPropertyChanged(nameof(CanCompletarContexto));
        OnPropertyChanged(nameof(CanConfirmarConclusao));
        CompletarContextoCommand.NotifyCanExecuteChanged();
        ConfirmarConclusaoCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedPecaChanged(ProducaoPecaDisponivelItem? value)
    {
        OnPropertyChanged(nameof(HasSelectedPeca));
        OnPropertyChanged(nameof(SelectedPecaDisplay));
        OnPropertyChanged(nameof(CanCompletarContexto));
        CompletarContextoCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedProximaFaseChanged(FaseProducaoItem? value)
    {
        OnPropertyChanged(nameof(ProximaFasePlaneadaDisplay));
        OnPropertyChanged(nameof(CanConfirmarConclusao));
        ConfirmarConclusaoCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsConclusaoSelectionActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(CanIniciarConclusao));
        OnPropertyChanged(nameof(CanEscolherProximaFase));
        OnPropertyChanged(nameof(CanConfirmarConclusao));
        PrepararConclusaoCommand.NotifyCanExecuteChanged();
        CancelarConclusaoCommand.NotifyCanExecuteChanged();
        ConfirmarConclusaoCommand.NotifyCanExecuteChanged();
    }

    partial void OnUtilizadorAtualIdChanged(int? value)
    {
        OnPropertyChanged(nameof(UtilizadorAtualDisplay));
        OnPropertyChanged(nameof(CanCompletarContexto));
        CompletarContextoCommand.NotifyCanExecuteChanged();
    }

    partial void OnUtilizadorAtualNomeChanged(string value)
    {
        OnPropertyChanged(nameof(UtilizadorAtualDisplay));
    }

    partial void OnIsSavingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCompletarContexto));
        OnPropertyChanged(nameof(CanIniciarConclusao));
        OnPropertyChanged(nameof(CanConfirmarParagem));
        OnPropertyChanged(nameof(CanConfirmarConclusao));
        CompletarContextoCommand.NotifyCanExecuteChanged();
        ConfirmarPausaCommand.NotifyCanExecuteChanged();
        PrepararConclusaoCommand.NotifyCanExecuteChanged();
        CancelarConclusaoCommand.NotifyCanExecuteChanged();
        ConfirmarConclusaoCommand.NotifyCanExecuteChanged();
    }

    partial void OnInfoMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasInfo));
    }

    /// <summary>
    /// Carrega a maquina, o evento pendente e as pecas disponiveis para completar contexto.
    /// </summary>
    /// <param name="maquinaId">Identificador da maquina a apresentar.</param>
    /// <returns>Tarefa assincrona da operacao de carregamento.</returns>
    public async Task LoadAsync(int maquinaId)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        IsConclusaoSelectionActive = false;

        try
        {
            await LoadUtilizadorAtualAsync();
            await RefreshIndustrialStateAsync(maquinaId);
            await LoadProximasFasesConclusaoAsync();
            Page = 1;
            if (NeedsContextSelection)
                await LoadPecasPageCoreAsync();
            else
                ClearPecasEncontradas();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Recarrega apenas o estado industrial visivel sem reiniciar a navegação da pagina.
    /// </summary>
    public async Task RefreshAsync()
    {
        if (Maquina is null || IsLoading || IsSaving || IsConclusaoSelectionActive)
            return;

        try
        {
            await RefreshIndustrialStateAsync(Maquina.Maquina_id);
            await LoadProximasFasesConclusaoAsync();

            if (NeedsContextSelection)
                await LoadPecasPageCoreAsync();
            else
                ClearPecasEncontradas();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private static async Task VoltarAsync()
    {
        await ShellNavigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task RecarregarAsync()
    {
        if (Maquina is null)
            return;

        await LoadAsync(Maquina.Maquina_id);
    }

    protected override async Task LoadPageAsync()
    {
        await ExecutePagedLoadAsync(LoadPecasPageCoreAsync);
    }

    [RelayCommand]
    private async Task PesquisarPecasAsync()
    {
        Page = 1;
        await LoadPageAsync();
    }

    [RelayCommand]
    private void SelecionarPeca(ProducaoPecaDisponivelItem? peca)
    {
        if (peca is null)
            return;

        SelectedPeca = peca;
    }

    [RelayCommand(CanExecute = nameof(CanCompletarContexto))]
    private async Task CompletarContextoAsync()
    {
        if (Maquina is null || EventoPendente is null || SelectedPeca is null || !UtilizadorAtualId.HasValue)
            return;

        IsSaving = true;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;

        try
        {
            await _industrialProducaoService.CompletarContextoAsync(
                EventoPendente.EventoMaquinaIndustrial_id,
                UtilizadorAtualId.Value,
                SelectedPeca.PecaId,
                Maquina.FaseDedicada_id);

            await _dialogService.ShowSuccessAsync(
                "Contexto registado",
                $"A maquina {Maquina.NumeroDisplay} ficou associada a {SelectedPeca.NumeroPecaDisplay}.");

            SelectedPeca = null;
            await LoadAsync(Maquina.Maquina_id);
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

    [RelayCommand(CanExecute = nameof(CanConfirmarParagem))]
    private async Task ConfirmarPausaAsync()
    {
        await ConfirmarParagemCoreAsync(false, "Paragem confirmada", "A maquina foi marcada como pausada.");
    }

    [RelayCommand(CanExecute = nameof(CanIniciarConclusao))]
    private void PrepararConclusao()
    {
        IsConclusaoSelectionActive = true;
    }

    [RelayCommand(CanExecute = nameof(CanConfirmarParagem))]
    private void CancelarConclusao()
    {
        IsConclusaoSelectionActive = false;
    }

    [RelayCommand(CanExecute = nameof(CanConfirmarConclusao))]
    private async Task ConfirmarConclusaoAsync()
    {
        await ConfirmarParagemCoreAsync(
            true,
            "Producao concluida",
            "O trabalho ativo da maquina foi concluido.",
            SelectedProximaFase?.FasesProducao_id);
    }

    private async Task LoadUtilizadorAtualAsync()
    {
        UtilizadorAtualId = _sessaoPersistidaService.TryGetCurrentUserId();
        UtilizadorAtualNome = string.Empty;

        if (!UtilizadorAtualId.HasValue)
            return;

        var utilizador = await _utilizadoresService.GetUtilizadorByIdAsync(UtilizadorAtualId.Value);
        UtilizadorAtualNome = utilizador.Nome;
    }

    private async Task LoadEventoPendenteAsync(int maquinaId)
    {
        EventoPendente = await _industrialProducaoService.GetEventoPendenteMaquinaAsync(maquinaId);
        InfoMessage = EventoPendente is null
            ? "Neste momento nao existe nenhum evento RUNNING/STOPPED pendente para esta maquina."
            : string.Empty;
    }

    private async Task LoadSessaoAtivaAsync(int maquinaId)
    {
        SessaoAtiva = await _industrialProducaoService.GetSessaoAtivaAsync(maquinaId);
    }

    private async Task RefreshIndustrialStateAsync(int maquinaId)
    {
        Maquina = await _maquinasService.GetByIdAsync(maquinaId)
            ?? throw new InvalidOperationException($"Nao foi possivel carregar a maquina {maquinaId}.");

        await LoadEventoPendenteAsync(maquinaId);
        await LoadSessaoAtivaAsync(maquinaId);
    }

    private async Task LoadProximasFasesConclusaoAsync()
    {
        ProximasFasesDisponiveis.Clear();
        SelectedProximaFase = null;

        if (!HasEventoStoppedPendente || SessaoAtiva is null)
        {
            IsConclusaoSelectionActive = false;
            OnPropertyChanged(nameof(CanIniciarConclusao));
            OnPropertyChanged(nameof(CanEscolherProximaFase));
            OnPropertyChanged(nameof(ProximaFasePlaneadaDisplay));
            OnPropertyChanged(nameof(CanConfirmarConclusao));
            PrepararConclusaoCommand.NotifyCanExecuteChanged();
            CancelarConclusaoCommand.NotifyCanExecuteChanged();
            ConfirmarConclusaoCommand.NotifyCanExecuteChanged();
            return;
        }

        _todasFases = await GetAllFasesAsync();
        foreach (var fase in _todasFases)
            ProximasFasesDisponiveis.Add(fase);

        SelectedProximaFase = ProximasFasesDisponiveis.FirstOrDefault(item => item.FasesProducao_id == SessaoAtiva.ProximaFasePlaneada_id)
            ?? ProximasFasesDisponiveis.FirstOrDefault(item => item.FasesProducao_id == SessaoAtiva.Fase_id)
            ?? ProximasFasesDisponiveis.FirstOrDefault();

        OnPropertyChanged(nameof(CanIniciarConclusao));
        OnPropertyChanged(nameof(CanEscolherProximaFase));
        OnPropertyChanged(nameof(ProximaFasePlaneadaDisplay));
        OnPropertyChanged(nameof(CanConfirmarConclusao));
        PrepararConclusaoCommand.NotifyCanExecuteChanged();
        CancelarConclusaoCommand.NotifyCanExecuteChanged();
        ConfirmarConclusaoCommand.NotifyCanExecuteChanged();
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
            .OrderBy(item => item.FasesProducao_id)
            .ToList();
    }

    private async Task LoadPecasPageCoreAsync()
    {
        if (!NeedsContextSelection)
        {
            ClearPecasEncontradas();
            return;
        }

        IsSearchingPecas = true;
        ErrorMessage = string.Empty;

        try
        {
            var pagina = await _pecasService.GetFilaTrabalhoAsync(Page, PageSize, PesquisaPeca, "Peca", Maquina?.FaseDedicada_id);

            PecasEncontradas.Clear();

            if (pagina?.Items is not null)
            {
                foreach (var peca in pagina.Items)
                    PecasEncontradas.Add(peca);
            }

            var totalItems = pagina?.TotalItems ?? 0;
            var totalPages = pagina?.TotalPages ?? 1;
            UpdatePagination(totalItems, totalPages);

            if (SelectedPeca is not null && !PecasEncontradas.Any(peca => peca.PecaId == SelectedPeca.PecaId))
                SelectedPeca = null;

            OnPropertyChanged(nameof(HasPecasEncontradas));
        }
        catch (Exception ex)
        {
            PecasEncontradas.Clear();
            UpdatePagination(0, 1);
            SelectedPeca = null;
            OnPropertyChanged(nameof(HasPecasEncontradas));
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSearchingPecas = false;
        }
    }

    private void ClearPecasEncontradas()
    {
        PecasEncontradas.Clear();
        UpdatePagination(0, 1);
        SelectedPeca = null;
        OnPropertyChanged(nameof(HasPecasEncontradas));
    }

    private async Task ConfirmarParagemCoreAsync(
        bool trabalhoConcluido,
        string tituloSucesso,
        string mensagemSucesso,
        int? proximaFaseId = null)
    {
        if (Maquina is null || EventoPendente is null)
            return;

        IsSaving = true;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;

        try
        {
            await _industrialProducaoService.ConfirmarParagemAsync(
                EventoPendente.EventoMaquinaIndustrial_id,
                trabalhoConcluido,
                proximaFaseId);

            await _dialogService.ShowSuccessAsync(tituloSucesso, mensagemSucesso);
            await LoadAsync(Maquina.Maquina_id);
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

    private static bool IsEventoRunning(IndustrialEventoDto? evento)
    {
        return string.Equals(evento?.EstadoMaquina, "RUNNING", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEventoStopped(IndustrialEventoDto? evento)
    {
        return string.Equals(evento?.EstadoMaquina, "STOPPED", StringComparison.OrdinalIgnoreCase);
    }
}
