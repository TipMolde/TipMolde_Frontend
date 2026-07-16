using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere o dashboard principal do frontend.
/// </summary>
/// <remarks>
/// Consolida hero do molde mais urgente, resumo executivo, planeamento
/// semanal e fluxo de rececao de material numa unica superficie de entrada.
/// </remarks>
public partial class DashboardViewModel : ObservableObject
{
    private const string ValorNaoDefinido = "Nao definido";
    private const int MaxMoldesRececaoInicial = 25;
    private const int MaxVerificacoesRececaoEmParalelo = 4;

    private readonly EncomendasService _encomendasService;
    private readonly MoldesService _moldesService;
    private readonly PecasService _pecasService;
    private readonly PedidosMaterialService _pedidosMaterialService;
    private readonly AuthorizationService _authorizationService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    private List<FilaGlobalMoldeItemDto> _todosMoldesPlanificacao = [];
    private bool _suppressSelectedMoldeRececaoChanged;
    private int _rececaoMaterialLoadVersion;

    /// <summary>
    /// Construtor do view model principal do dashboard.
    /// </summary>
    /// <param name="encomendasService">Servico para consultar encomendas e fila global.</param>
    /// <param name="moldesService">Servico para consultar detalhe e dashboard dos moldes.</param>
    /// <param name="pecasService">Servico para consultar e atualizar pecas pendentes.</param>
    /// <param name="pedidosMaterialService">Servico para consultar pedidos de material.</param>
    /// <param name="authorizationService">Servico para validar acesso a areas do dashboard.</param>
    /// <param name="dialogService">Servico para apresentar dialogs ao utilizador.</param>
    /// <param name="navigationService">Servico de navegacao principal da aplicacao.</param>
    public DashboardViewModel(
        EncomendasService encomendasService,
        MoldesService moldesService,
        PecasService pecasService,
        PedidosMaterialService pedidosMaterialService,
        AuthorizationService authorizationService,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        _encomendasService = encomendasService;
        _moldesService = moldesService;
        _pecasService = pecasService;
        _pedidosMaterialService = pedidosMaterialService;
        _authorizationService = authorizationService;
        _dialogService = dialogService;
        _navigationService = navigationService;

        PecasPendentesRececao.CollectionChanged += OnPecasPendentesRececaoCollectionChanged;
    }

    public ObservableCollection<MoldeRececaoOption> MoldesRececaoDisponiveis { get; } = new();
    public ObservableCollection<SelectablePecaRececaoItem> PecasPendentesRececao { get; } = new();
    public ObservableCollection<FilaGlobalMoldeItemDto> MoldesPlanificacao { get; } = new();
    public ObservableCollection<DashboardPlanificacaoDiaItem> PlanificacaoDias { get; } = new();
    public ObservableCollection<DashboardPlanificacaoSemanaItem> PlanificacaoSemanas { get; } = new();
    public ObservableCollection<FilaGlobalMoldeItemDto> MoldesDiaSelecionado { get; } = new();

    [ObservableProperty]
    private bool isLoadingHero;

    [ObservableProperty]
    private string heroErrorMessage = string.Empty;

    [ObservableProperty]
    private MoldeDto? moldeMaisProximo;

    [ObservableProperty]
    private EncomendaResumoDto? encomendaMaisProxima;

    [ObservableProperty]
    private EncomendaMoldeDto? entregaMoldeMaisProxima;

    [ObservableProperty]
    private MoldeCicloVidaDashboardDto? dashboardMaisProximo;

    [ObservableProperty]
    private DateTime semanaInicialPlanificacao = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);

    [ObservableProperty]
    private int numeroSemanasPlanificacao = 4;

    [ObservableProperty]
    private int? totalMoldesPorEntregar;

    [ObservableProperty]
    private decimal? taxaConclusao;

    [ObservableProperty]
    private int? encomendasConcluidasUltimosTresMeses;

    [ObservableProperty]
    private int? moldesComAtraso;

    [ObservableProperty]
    private bool canUseRececaoMaterial;

    [ObservableProperty]
    private bool isLoadingRececaoMaterial;

    [ObservableProperty]
    private bool isSavingRececaoMaterial;

    [ObservableProperty]
    private string rececaoMaterialErrorMessage = string.Empty;

    [ObservableProperty]
    private MoldeRececaoOption? selectedMoldeRececao;

    [ObservableProperty]
    private DashboardPlanificacaoDiaItem? selectedPlanificacaoDia;

    [ObservableProperty]
    private double planificacaoCellSize = 132;

    public bool HasHeroError => !string.IsNullOrWhiteSpace(HeroErrorMessage);
    public bool HasMoldeEntregaDashboard =>
        MoldeMaisProximo is not null &&
        EncomendaMaisProxima is not null &&
        EntregaMoldeMaisProxima is not null &&
        DashboardMaisProximo is not null;
    public bool HasNoMoldeEntregaDashboard => !IsLoadingHero && !HasHeroError && !HasMoldeEntregaDashboard;
    public bool HasPlanificacao => HasMoldesPlanificacao && PlanificacaoSemanas.Count > 0;
    public bool HasMoldesPlanificacao => MoldesPlanificacao.Count > 0;
    public bool HasNoPlanificacao => !IsLoadingHero && !HasMoldesPlanificacao;
    public bool HasSelectedPlanificacaoDia => SelectedPlanificacaoDia is not null;
    public bool HasNoMoldesDiaSelecionado => HasSelectedPlanificacaoDia && MoldesDiaSelecionado.Count == 0;
    public bool HasRececaoMaterialError => !string.IsNullOrWhiteSpace(RececaoMaterialErrorMessage);
    public bool HasMoldesRececaoDisponiveis => MoldesRececaoDisponiveis.Count > 0;
    public bool HasNoMoldesRececaoDisponiveis => CanUseRececaoMaterial && !IsLoadingRececaoMaterial && MoldesRececaoDisponiveis.Count == 0;
    public bool HasSelectedMoldeRececao => SelectedMoldeRececao is not null;
    public bool HasPecasPendentesRececao => PecasPendentesRececao.Count > 0;
    public bool HasNoPecasPendentesRececao => HasSelectedMoldeRececao && !IsLoadingRececaoMaterial && !HasRececaoMaterialError && PecasPendentesRececao.Count == 0;
    public bool CanRegistarChegadaMaterial => CanUseRececaoMaterial &&
                                              SelectedMoldeRececao is not null &&
                                              !IsLoadingRececaoMaterial &&
                                              !IsSavingRececaoMaterial &&
                                              PecasPendentesRececao.Any(item => item.IsSelected);

    public string NomeMoldeMaisProximoDisplay => FirstNonEmpty(MoldeMaisProximo?.Nome, MoldeMaisProximo?.Numero, EntregaMoldeMaisProxima?.NumeroMolde);
    public string NumeroMoldeMaisProximoDisplay => FirstNonEmpty(MoldeMaisProximo?.Numero, EntregaMoldeMaisProxima?.NumeroMolde);
    public string ClienteMoldeMaisProximoDisplay => FirstNonEmpty(EncomendaMaisProxima?.NomeClienteDisplay);
    public string DataEntregaMoldeMaisProximoDisplay => EntregaMoldeMaisProxima is null
        ? ValorNaoDefinido
        : EntregaMoldeMaisProxima.DataEntregaPrevista.ToString("dd/MM/yyyy");
    public string PercentagemConclusaoMaisProximoDisplay => DashboardMaisProximo is null
        ? ValorNaoDefinido
        : $"{DashboardMaisProximo.PercentagemConclusao:0.##}%";
    public string IntervaloPlanificacaoDisplay => $"{SemanaInicialPlanificacao:dd/MM/yyyy} - {SemanaFinalPlanificacao:dd/MM/yyyy}";
    public string NumeroSemanasPlanificacaoDisplay => NumeroSemanasPlanificacao == 1
        ? "1 semana"
        : $"{NumeroSemanasPlanificacao} semanas";
    public string PlanificacaoResumoDisplay => HasMoldesPlanificacao
        ? $"{MoldesPlanificacao.Count} molde(s) nas semanas visiveis"
        : "Sem moldes nas semanas selecionadas.";
    public string PlanificacaoVaziaDisplay => "Nao existem moldes com EncomendaMolde nas semanas selecionadas.";
    public string SelectedPlanificacaoDiaDisplay => SelectedPlanificacaoDia is null
        ? "Seleciona um dia para ver os moldes desse intervalo."
        : $"{SelectedPlanificacaoDia.DiaSemanaDisplay}, {SelectedPlanificacaoDia.DataDisplay}";
    public string SelectedPlanificacaoDiaResumoDisplay => SelectedPlanificacaoDia is null
        ? string.Empty
        : SelectedPlanificacaoDia.Moldes.Count == 1
            ? "1 molde planeado para este dia."
            : $"{SelectedPlanificacaoDia.Moldes.Count} moldes planeados para este dia.";
    public DateTime SemanaFinalPlanificacao => SemanaInicialPlanificacao.Date.AddDays((Math.Clamp(NumeroSemanasPlanificacao, 1, 8) * 7) - 1);
    public string TotalMoldesPorEntregarDisplay => TotalMoldesPorEntregar?.ToString() ?? ValorNaoDefinido;
    public string TaxaConclusaoDisplay => TaxaConclusao.HasValue
        ? $"{TaxaConclusao.Value:0.##}%"
        : ValorNaoDefinido;
    public string EncomendasConcluidasUltimosTresMesesDisplay => EncomendasConcluidasUltimosTresMeses?.ToString() ?? ValorNaoDefinido;
    public string MoldesComAtrasoDisplay => MoldesComAtraso?.ToString() ?? ValorNaoDefinido;
    public string IntervaloUltimosTresMesesDisplay => $"{DateTime.Today.AddMonths(-3):dd/MM/yyyy} - {DateTime.Today:dd/MM/yyyy}";
    public string RececaoMaterialButtonText => IsSavingRececaoMaterial
        ? "A registar chegada..."
        : "Registar chegada de material";
    public string SelectedMoldeRececaoResumo => SelectedMoldeRececao is null
        ? "Seleciona um molde com pedido de material ativo para marcar as peças que chegaram."
        : $"Encomenda {SelectedMoldeRececao.EncomendaDisplay} | entrega {SelectedMoldeRececao.DataEntregaDisplay}";
    public string PecasRececaoSelectionSummary
    {
        get
        {
            if (PecasPendentesRececao.Count == 0)
                return "Não existem peças pendentes para este molde.";

            var selecionadas = PecasPendentesRececao.Count(item => item.IsSelected);
            return selecionadas == 0
                ? $"{PecasPendentesRececao.Count} peça(s) pendente(s). Seleciona as que chegaram."
                : $"{selecionadas} de {PecasPendentesRececao.Count} peça(s) selecionada(s).";
        }
    }

    partial void OnIsLoadingHeroChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoPlanificacao));
        OnPropertyChanged(nameof(HasSelectedPlanificacaoDia));
        OnPropertyChanged(nameof(HasNoMoldesRececaoDisponiveis));
    }

    partial void OnHeroErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasHeroError));
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoPlanificacao));
        OnPropertyChanged(nameof(HasSelectedPlanificacaoDia));
    }

    partial void OnMoldeMaisProximoChanged(MoldeDto? value)
    {
        OnPropertyChanged(nameof(HasMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoPlanificacao));
        OnPropertyChanged(nameof(HasSelectedPlanificacaoDia));
        OnPropertyChanged(nameof(NomeMoldeMaisProximoDisplay));
        OnPropertyChanged(nameof(NumeroMoldeMaisProximoDisplay));
        AbrirDashboardMoldeCommand.NotifyCanExecuteChanged();
    }

    partial void OnEncomendaMaisProximaChanged(EncomendaResumoDto? value)
    {
        OnPropertyChanged(nameof(HasMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoPlanificacao));
        OnPropertyChanged(nameof(HasSelectedPlanificacaoDia));
        OnPropertyChanged(nameof(ClienteMoldeMaisProximoDisplay));
    }

    partial void OnEntregaMoldeMaisProximaChanged(EncomendaMoldeDto? value)
    {
        OnPropertyChanged(nameof(HasMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoPlanificacao));
        OnPropertyChanged(nameof(HasSelectedPlanificacaoDia));
        OnPropertyChanged(nameof(NumeroMoldeMaisProximoDisplay));
        OnPropertyChanged(nameof(DataEntregaMoldeMaisProximoDisplay));
        AbrirDashboardMoldeCommand.NotifyCanExecuteChanged();
    }

    partial void OnDashboardMaisProximoChanged(MoldeCicloVidaDashboardDto? value)
    {
        OnPropertyChanged(nameof(HasMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoPlanificacao));
        OnPropertyChanged(nameof(HasSelectedPlanificacaoDia));
        OnPropertyChanged(nameof(PercentagemConclusaoMaisProximoDisplay));
    }

    partial void OnTotalMoldesPorEntregarChanged(int? value) => OnPropertyChanged(nameof(TotalMoldesPorEntregarDisplay));
    partial void OnTaxaConclusaoChanged(decimal? value) => OnPropertyChanged(nameof(TaxaConclusaoDisplay));
    partial void OnEncomendasConcluidasUltimosTresMesesChanged(int? value) => OnPropertyChanged(nameof(EncomendasConcluidasUltimosTresMesesDisplay));
    partial void OnMoldesComAtrasoChanged(int? value) => OnPropertyChanged(nameof(MoldesComAtrasoDisplay));
}
