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
    private readonly MaquinasService _maquinasService;
    private readonly IndustrialProducaoService _industrialProducaoService;
    private readonly PecasService _pecasService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly UtilizadoresService _utilizadoresService;
    private readonly IDialogService _dialogService;

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
        MaquinasService maquinasService,
        IndustrialProducaoService industrialProducaoService,
        PecasService pecasService,
        SessaoPersistidaService sessaoPersistidaService,
        UtilizadoresService utilizadoresService,
        IDialogService dialogService)
    {
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
            CompletarContextoCommand.NotifyCanExecuteChanged();
        };
    }

    public ObservableCollection<ProducaoPecaDisponivelItem> PecasEncontradas { get; } = new();

    [ObservableProperty]
    private MaquinaItem? maquina;

    [ObservableProperty]
    private IndustrialEventoDto? eventoPendente;

    [ObservableProperty]
    private ProducaoPecaDisponivelItem? selectedPeca;

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
    private string infoMessage = string.Empty;

    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool HasMaquina => Maquina is not null;
    public bool HasEventoPendente => EventoPendente is not null;
    public bool HasPecasEncontradas => PecasEncontradas.Count > 0;
    public bool HasSelectedPeca => SelectedPeca is not null;
    public bool CanCompletarContexto => !IsLoading
                                        && !IsSaving
                                        && Maquina is not null
                                        && EventoPendente is not null
                                        && SelectedPeca is not null
                                        && UtilizadorAtualId.HasValue;

    public string MaquinaTitulo => Maquina is null
        ? "Maquina"
        : $"{Maquina.NumeroDisplay} - {Maquina.NomeModeloDisplay}";

    public string UtilizadorAtualDisplay => UtilizadorAtualId.HasValue
        ? $"{UtilizadorAtualNome} ({UtilizadorAtualId.Value})"
        : "Utilizador atual indisponivel";

    public string EventoPendenteDisplay => EventoPendente is null
        ? "Sem pedido pendente para completar contexto."
        : $"{EventoPendente.EstadoMaquinaDisplay} recebido em {EventoPendente.OccurredAtDisplay}";

    public string SelectedPecaDisplay => SelectedPeca is null
        ? "Nenhuma peca selecionada"
        : $"{SelectedPeca.NumeroPecaDisplay} - {SelectedPeca.DesignacaoDisplay}";

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
        OnPropertyChanged(nameof(EventoPendenteDisplay));
        OnPropertyChanged(nameof(CanCompletarContexto));
        CompletarContextoCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedPecaChanged(ProducaoPecaDisponivelItem? value)
    {
        OnPropertyChanged(nameof(HasSelectedPeca));
        OnPropertyChanged(nameof(SelectedPecaDisplay));
        OnPropertyChanged(nameof(CanCompletarContexto));
        CompletarContextoCommand.NotifyCanExecuteChanged();
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
        CompletarContextoCommand.NotifyCanExecuteChanged();
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

        try
        {
            Maquina = await _maquinasService.GetByIdAsync(maquinaId)
                ?? throw new InvalidOperationException($"Nao foi possivel carregar a maquina {maquinaId}.");

            await LoadUtilizadorAtualAsync();
            await LoadEventoPendenteAsync(maquinaId);
            Page = 1;
            await LoadPecasPageCoreAsync();
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
            await LoadEventoPendenteAsync(Maquina.Maquina_id);
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
        EventoPendente = null;
        InfoMessage = string.Empty;

        var primeiraPagina = await _industrialProducaoService.GetEventosPendentesAsync(1, 100);
        var eventos = primeiraPagina?.Items ?? [];

        EventoPendente = eventos
            .Where(e => e.Maquina_id == maquinaId)
            .Where(e => string.Equals(e.EstadoMaquina, "RUNNING", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.OccurredAt)
            .FirstOrDefault();

        if (EventoPendente is null)
            InfoMessage = "Neste momento nao existe nenhum RUNNING pendente para esta maquina.";
    }

    private async Task LoadPecasPageCoreAsync()
    {
        IsSearchingPecas = true;
        ErrorMessage = string.Empty;

        try
        {
            var pagina = await _pecasService.GetFilaTrabalhoAsync(Page, PageSize, PesquisaPeca, "Peca");

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
}
