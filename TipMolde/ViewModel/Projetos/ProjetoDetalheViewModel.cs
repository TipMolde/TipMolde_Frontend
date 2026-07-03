using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class ProjetoDetalheViewModel : ObservableObject
{
    private readonly ProjetosService _projetosService;
    private readonly RevisoesService _revisoesService;
    private readonly RegistosTempoProjetoService _registosTempoProjetoService;
    private readonly AuthorizationService _authorizationService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly IDialogService _dialogService;
    private readonly IFilePickerService _filePickerService;
    private bool _roleLoaded;
    private int? _currentUserId;

    public ProjetoDetalheViewModel(
        ProjetosService projetosService,
        RevisoesService revisoesService,
        RegistosTempoProjetoService registosTempoProjetoService,
        AuthorizationService authorizationService,
        SessaoPersistidaService sessaoPersistidaService,
        IDialogService dialogService,
        IFilePickerService filePickerService)
    {
        _projetosService = projetosService;
        _revisoesService = revisoesService;
        _registosTempoProjetoService = registosTempoProjetoService;
        _authorizationService = authorizationService;
        _sessaoPersistidaService = sessaoPersistidaService;
        _dialogService = dialogService;
        _filePickerService = filePickerService;
    }

    [ObservableProperty]
    private int projetoId;

    [ObservableProperty]
    private ProjetoComRevisoesDto? projeto;

    [ObservableProperty]
    private bool isAdmin;

    [ObservableProperty]
    private bool canManageTempo;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isCreatingRevisao;

    [ObservableProperty]
    private bool isSendingResposta;

    [ObservableProperty]
    private bool isRegisteringTempo;

    [ObservableProperty]
    private TimeSpan tempoRegistadoTotal;

    [ObservableProperty]
    private string tempoSessaoAtiva = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public ObservableCollection<RevisaoDto> Revisoes { get; } = new();
    public ObservableCollection<RegistoTempoProjetoDto> RegistosTempo { get; } = new();

    public bool HasProjeto => Projeto is not null;
    public bool HasRevisoes => Revisoes.Count > 0;
    public bool HasRegistosTempo => RegistosTempo.Count > 0;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool CanAccessTempo => CanManageTempo && HasProjeto;
    public bool CanCreateRevisao => IsAdmin && HasProjeto && !HasOpenOrApprovedRevision(Projeto);
    public string ProjetoTituloDisplay => Projeto?.NomeProjetoDisplay ?? "Projeto";
    public string ProjetoSubtituloDisplay => Projeto is null
        ? "Sem contexto carregado."
        : $"{Projeto.MoldeDisplay} | {Projeto.SoftwareUtilizadoDisplay} | {Projeto.TipoProjetoDisplay}";
    public string ProjetoCaminhoDisplay => Projeto?.CaminhoPastaServidorDisplay ?? "Caminho nao definido";
    public string RevisoesResumoDisplay => Projeto?.RevisoesResumoDisplay ?? "Sem revisoes associadas";
    public string EmptyRevisoesMessage => "Ainda nao existem revisoes para este projeto.";
    public string EmptyTempoMessage => CanAccessTempo && _currentUserId.HasValue
        ? "Ainda nao existem registos de tempo para este projeto."
        : "Apenas o gestor de desenho ou o administrador pode consultar o historico de tempo.";
    public string TempoTotalDisplay => FormatDuration(TempoRegistadoTotal);
    public string TempoSessaoAtivaDisplay => string.IsNullOrWhiteSpace(TempoSessaoAtiva)
        ? "Sem sessao ativa"
        : TempoSessaoAtiva;

    partial void OnProjetoChanged(ProjetoComRevisoesDto? value)
    {
        OnPropertyChanged(nameof(HasProjeto));
        OnPropertyChanged(nameof(ProjetoTituloDisplay));
        OnPropertyChanged(nameof(ProjetoSubtituloDisplay));
        OnPropertyChanged(nameof(ProjetoCaminhoDisplay));
        OnPropertyChanged(nameof(RevisoesResumoDisplay));
        OnPropertyChanged(nameof(CanAccessTempo));
        OnPropertyChanged(nameof(CanCreateRevisao));
        OnPropertyChanged(nameof(EmptyTempoMessage));
    }

    partial void OnCanManageTempoChanged(bool value)
    {
        OnPropertyChanged(nameof(CanAccessTempo));
        OnPropertyChanged(nameof(EmptyTempoMessage));
    }

    partial void OnIsAdminChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCreateRevisao));
    }

    partial void OnTempoRegistadoTotalChanged(TimeSpan value) => OnPropertyChanged(nameof(TempoTotalDisplay));
    partial void OnTempoSessaoAtivaChanged(string value) => OnPropertyChanged(nameof(TempoSessaoAtivaDisplay));
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
}
