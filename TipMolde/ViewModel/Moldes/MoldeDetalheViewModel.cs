using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using TipMolde.Domain.Enums;
using TipMolde.Helper;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel.Helpers;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

/// <summary>
/// Apresenta o detalhe completo de um molde, incluindo ficha tecnica, pecas, projetos e revisoes.
/// </summary>
public partial class MoldeDetalheViewModel : PaginatedViewModel
{
    private const string ValorNaoDefinido = "Nao definido";
    private const string DialogCancel = "Cancelar";
    private const string EstadoPausado = "PAUSADO";
    private const string EstadoConcluido = "CONCLUIDO";

    private readonly MoldesService _moldesService;
    private readonly ProjetosService _projetosService;
    private readonly RevisoesService _revisoesService;
    private readonly RegistosTempoProjetoService _registosTempoProjetoService;
    private readonly RegistosProducaoService _registosProducaoService;
    private readonly PecasService _pecasService;
    private readonly AuthorizationService _authorizationService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly IDestinationFolderPickerService _destinationFolderPickerService;
    private readonly IDialogService _dialogService;
    private readonly IFilePickerService _filePickerService;
    private readonly INavigationService _navigationService;
    private bool _roleLoaded;
    private bool _suspendSelectedProjetoLoad;
    private int? _currentUserId;

    /// <summary>
    /// Construtor do view model de detalhe de molde.
    /// </summary>
    /// <param name="moldesService">Servico usado para carregar o molde e respetivo dashboard.</param>
    /// <param name="projetosService">Servico usado para consultar projetos associados ao molde.</param>
    /// <param name="revisoesService">Servico usado para consultar e criar revisoes.</param>
    /// <param name="registosTempoProjetoService">Servico usado para consultar registos de tempo de desenho.</param>
    /// <param name="registosProducaoService">Servico usado para consultar tempos acumulados das pecas.</param>
    /// <param name="pecasService">Servico usado para gerir pecas do molde.</param>
    /// <param name="authorizationService">Servico usado para validar permissoes do utilizador atual.</param>
    /// <param name="sessaoPersistidaService">Servico usado para obter a sessao autenticada.</param>
    /// <param name="destinationFolderPickerService">Servico usado para escolher pastas de exportacao.</param>
    /// <param name="dialogService">Servico usado para apresentar feedback ao utilizador.</param>
    /// <param name="filePickerService">Servico usado para selecionar ficheiros associados ao molde.</param>
    /// <param name="navigationService">Servico usado para navegacao programatica entre paginas.</param>
    public MoldeDetalheViewModel(
        MoldesService moldesService,
        ProjetosService projetosService,
        RevisoesService revisoesService,
        RegistosTempoProjetoService registosTempoProjetoService,
        RegistosProducaoService registosProducaoService,
        PecasService pecasService,
        AuthorizationService authorizationService,
        SessaoPersistidaService sessaoPersistidaService,
        IDestinationFolderPickerService destinationFolderPickerService,
        IDialogService dialogService,
        IFilePickerService filePickerService,
        INavigationService navigationService)
    {
        _moldesService = moldesService;
        _projetosService = projetosService;
        _revisoesService = revisoesService;
        _registosTempoProjetoService = registosTempoProjetoService;
        _registosProducaoService = registosProducaoService;
        _pecasService = pecasService;
        _authorizationService = authorizationService;
        _sessaoPersistidaService = sessaoPersistidaService;
        _destinationFolderPickerService = destinationFolderPickerService;
        _dialogService = dialogService;
        _filePickerService = filePickerService;
        _navigationService = navigationService;
        PageSize = 8;
    }

    [ObservableProperty]
    private int moldeId;

    [ObservableProperty]
    private string numero = string.Empty;

    [ObservableProperty]
    private string numeroMoldeCliente = string.Empty;

    [ObservableProperty]
    private string nome = string.Empty;

    [ObservableProperty]
    private string descricao = string.Empty;

    [ObservableProperty]
    private int numero_cavidades;

    [ObservableProperty]
    private string tipoPedido = string.Empty;

    [ObservableProperty]
    private string imagemCapaSource = MoldeImageSourceHelper.FallbackSource;

    [ObservableProperty]
    private decimal? largura;

    [ObservableProperty]
    private decimal? comprimento;

    [ObservableProperty]
    private decimal? altura;

    [ObservableProperty]
    private decimal? pesoEstimado;

    [ObservableProperty]
    private string tipoInjecao = string.Empty;

    [ObservableProperty]
    private string sistemaInjecao = string.Empty;

    [ObservableProperty]
    private decimal? contracao;

    [ObservableProperty]
    private string acabamentoPeca = string.Empty;

    [ObservableProperty]
    private CorMolde? cor;

    [ObservableProperty]
    private string materialMacho = string.Empty;

    [ObservableProperty]
    private string materialCavidade = string.Empty;

    [ObservableProperty]
    private string materialMovimentos = string.Empty;

    [ObservableProperty]
    private string materialInjecao = string.Empty;

    [ObservableProperty]
    private MoldeCicloVidaDashboardDto? dashboard;

    [ObservableProperty]
    private bool isAdmin;

    [ObservableProperty]
    private bool isGeneratingPdf;

    [ObservableProperty]
    private bool canManagePieces;

    [ObservableProperty]
    private bool isLoadingProjeto;

    [ObservableProperty]
    private bool isCreatingRevisao;

    [ObservableProperty]
    private bool isSendingResposta;

    [ObservableProperty]
    private bool isRegisteringTempo;

    [ObservableProperty]
    private TimeSpan tempoRegistadoTotal;

    [ObservableProperty]
    private TimeSpan tempoTotalPecas;

    [ObservableProperty]
    private string tempoSessaoAtiva = string.Empty;

    [ObservableProperty]
    private ProjetoDto? selectedProjeto;

    [ObservableProperty]
    private ProjetoComRevisoesDto? projetoAtivo;

    [ObservableProperty]
    private string searchTerm = string.Empty;

    public ObservableCollection<DesenhoMoldeItem> Moldes { get; } = new();
    public ObservableCollection<PecaDto> Pecas { get; } = new();
    public ObservableCollection<ProjetoDto> Projetos { get; } = new();
    public ObservableCollection<RevisaoDto> Revisoes { get; } = new();
    public ObservableCollection<RegistoTempoProjetoDto> RegistosTempo { get; } = new();

    public bool HasMoldes => Moldes.Count > 0;
    public bool HasDashboard => Dashboard is not null;
    public bool HasPecas => Pecas.Count > 0;
    public bool HasProjetos => Projetos.Count > 0;
    public bool HasRevisoes => Revisoes.Count > 0;
    public bool HasRegistosTempo => RegistosTempo.Count > 0;
    public bool HasProjetoAtivo => ProjetoAtivo is not null;
    public bool CanAccessTempoProjeto => CanManagePieces && HasProjetoAtivo;
    public bool CanAddPeca => CanManagePieces && HasProjetoAtivo && !HasOpenOrApprovedRevision(ProjetoAtivo);
    public bool HasProjetoContexto => SelectedProjeto is not null;
    public string NumeroDisplay => string.IsNullOrWhiteSpace(Numero) ? ValorNaoDefinido : Numero;
    public string NumeroMoldeClienteDisplay => string.IsNullOrWhiteSpace(NumeroMoldeCliente) ? ValorNaoDefinido : NumeroMoldeCliente;
    public string NomeDisplay => string.IsNullOrWhiteSpace(Nome) ? ValorNaoDefinido : Nome;
    public string DescricaoDisplay => string.IsNullOrWhiteSpace(Descricao) ? ValorNaoDefinido : Descricao;
    public string TipoPedidoDisplay => string.IsNullOrWhiteSpace(TipoPedido) ? ValorNaoDefinido : TipoPedido;
    public string LarguraDisplay => FormatDecimal(Largura);
    public string ComprimentoDisplay => FormatDecimal(Comprimento);
    public string AlturaDisplay => FormatDecimal(Altura);
    public string PesoEstimadoDisplay => FormatDecimal(PesoEstimado);
    public string TipoInjecaoDisplay => NormalizeTexto(TipoInjecao);
    public string SistemaInjecaoDisplay => NormalizeTexto(SistemaInjecao);
    public string ContracaoDisplay => FormatDecimal(Contracao);
    public string AcabamentoPecaDisplay => NormalizeTexto(AcabamentoPeca);
    public string CorDisplay => Cor?.ToString() ?? ValorNaoDefinido;
    public string MaterialMachoDisplay => NormalizeTexto(MaterialMacho);
    public string MaterialCavidadeDisplay => NormalizeTexto(MaterialCavidade);
    public string MaterialMovimentosDisplay => NormalizeTexto(MaterialMovimentos);
    public string MaterialInjecaoDisplay => NormalizeTexto(MaterialInjecao);
    public string PercentagemConclusaoDisplay => Dashboard is null ? ValorNaoDefinido : $"{Dashboard.PercentagemConclusao:0.##}%";
    public string TempoTotalPecasDisplay => FormatDuration(TempoTotalPecas);
    public int DistribuicaoTotal => Dashboard is null ? 0 : Dashboard.Maquinacao + Dashboard.Erosao + Dashboard.Montagem + Dashboard.MaterialPendente + Dashboard.EmEspera;
    public string PdfButtonText => IsGeneratingPdf ? "A gerar PDF..." : "Gerar PDF";
    public bool CanGeneratePdf => IsAdmin && HasDashboard && !IsGeneratingPdf;
    public string MaquinacaoDistribuicaoDisplay => BuildDistribuicaoDisplay(Dashboard?.Maquinacao ?? 0);
    public string ErosaoDistribuicaoDisplay => BuildDistribuicaoDisplay(Dashboard?.Erosao ?? 0);
    public string MontagemDistribuicaoDisplay => BuildDistribuicaoDisplay(Dashboard?.Montagem ?? 0);
    public string MaterialPendenteDistribuicaoDisplay => BuildDistribuicaoDisplay(Dashboard?.MaterialPendente ?? 0);
    public string EmEsperaDistribuicaoDisplay => BuildDistribuicaoDisplay(Dashboard?.EmEspera ?? 0);
    public static string EmptyPecasMessage => "Este molde ainda nao tem pecas registadas.";
    public string EmptyProjetosMessage => "Este molde ainda nao tem projetos de desenho associados.";
    public string EmptyProjetosMessageDisplay => HasProjetos ? string.Empty : EmptyProjetosMessage;
    public string EmptyRevisoesMessage => "Ainda nao existem revisoes para este projeto.";
    public string EmptyTempoMessage => CanManagePieces && _currentUserId.HasValue
        ? "Ainda nao existem registos de tempo para este projeto."
        : "Apenas o gestor de desenho ou o administrador pode consultar o historico de tempo.";
    public string ProjetoSectionDescription => HasProjetos
        ? "Escolhe um projeto para ver as revisoes feitas ao cliente e o tempo registado."
        : "Ainda nao existe um projeto de desenho associado a este molde.";
    public string TempoTotalDisplay => FormatDuration(TempoRegistadoTotal);
    public string TempoSessaoAtivaDisplay => string.IsNullOrWhiteSpace(TempoSessaoAtiva)
        ? "Sem sessao ativa"
        : TempoSessaoAtiva;
    public string ProjetoAtivoResumoDisplay => ProjetoAtivo is null
        ? "Seleciona um projeto para ver o detalhe."
        : $"{ProjetoAtivo.SoftwareUtilizadoDisplay} | {ProjetoAtivo.TipoProjetoDisplay}";
    public string ProjetoAtivoCaminhoDisplay => ProjetoAtivo?.CaminhoPastaServidorDisplay ?? ValorNaoDefinido;
    public string RevisoesResumoDisplay => ProjetoAtivo?.RevisoesResumoDisplay ?? "Sem revisoes";
    public string TempoResumoDisplay => HasRegistosTempo
        ? $"{RegistosTempo.Count} registos | total {TempoTotalDisplay}"
        : TempoSessaoAtivaDisplay;
    public bool CanEditMolde => IsAdmin;
    public string PecasSectionDescription => CanManagePieces
        ? "Edita prioridades, quantidades e remove pecas erradas diretamente a partir deste detalhe."
        : "Consulta as pecas registadas para este molde.";

    partial void OnNumeroChanged(string value) => OnPropertyChanged(nameof(NumeroDisplay));
    partial void OnNumeroMoldeClienteChanged(string value) => OnPropertyChanged(nameof(NumeroMoldeClienteDisplay));
    partial void OnNomeChanged(string value) => OnPropertyChanged(nameof(NomeDisplay));
    partial void OnDescricaoChanged(string value) => OnPropertyChanged(nameof(DescricaoDisplay));
    partial void OnTipoPedidoChanged(string value) => OnPropertyChanged(nameof(TipoPedidoDisplay));
    partial void OnLarguraChanged(decimal? value) => NotifyFichaTecnicaChanged();
    partial void OnComprimentoChanged(decimal? value) => NotifyFichaTecnicaChanged();
    partial void OnAlturaChanged(decimal? value) => NotifyFichaTecnicaChanged();
    partial void OnPesoEstimadoChanged(decimal? value) => NotifyFichaTecnicaChanged();
    partial void OnTipoInjecaoChanged(string value) => NotifyFichaTecnicaChanged();
    partial void OnSistemaInjecaoChanged(string value) => NotifyFichaTecnicaChanged();
    partial void OnContracaoChanged(decimal? value) => NotifyFichaTecnicaChanged();
    partial void OnAcabamentoPecaChanged(string value) => NotifyFichaTecnicaChanged();
    partial void OnCorChanged(CorMolde? value) => NotifyFichaTecnicaChanged();
    partial void OnMaterialMachoChanged(string value) => NotifyFichaTecnicaChanged();
    partial void OnMaterialCavidadeChanged(string value) => NotifyFichaTecnicaChanged();
    partial void OnMaterialMovimentosChanged(string value) => NotifyFichaTecnicaChanged();
    partial void OnMaterialInjecaoChanged(string value) => NotifyFichaTecnicaChanged();
    partial void OnDashboardChanged(MoldeCicloVidaDashboardDto? value)
    {
        OnPropertyChanged(nameof(HasDashboard));
        OnPropertyChanged(nameof(PercentagemConclusaoDisplay));
        OnPropertyChanged(nameof(DistribuicaoTotal));
        OnPropertyChanged(nameof(MaquinacaoDistribuicaoDisplay));
        OnPropertyChanged(nameof(ErosaoDistribuicaoDisplay));
        OnPropertyChanged(nameof(MontagemDistribuicaoDisplay));
        OnPropertyChanged(nameof(MaterialPendenteDistribuicaoDisplay));
        OnPropertyChanged(nameof(EmEsperaDistribuicaoDisplay));
        OnPropertyChanged(nameof(CanGeneratePdf));
        GerarPdfCommand.NotifyCanExecuteChanged();
    }
    partial void OnIsAdminChanged(bool value)
    {
        OnPropertyChanged(nameof(CanGeneratePdf));
        OnPropertyChanged(nameof(CanEditMolde));
        GerarPdfCommand.NotifyCanExecuteChanged();
    }
    partial void OnIsGeneratingPdfChanged(bool value)
    {
        OnPropertyChanged(nameof(CanGeneratePdf));
        OnPropertyChanged(nameof(PdfButtonText));
        GerarPdfCommand.NotifyCanExecuteChanged();
    }
    partial void OnCanManagePiecesChanged(bool value)
    {
        OnPropertyChanged(nameof(PecasSectionDescription));
        OnPropertyChanged(nameof(EmptyTempoMessage));
        OnPropertyChanged(nameof(CanAccessTempoProjeto));
        OnPropertyChanged(nameof(CanAddPeca));
    }
    partial void OnIsLoadingProjetoChanged(bool value)
    {
        OnPropertyChanged(nameof(HasProjetoContexto));
    }
    partial void OnTempoRegistadoTotalChanged(TimeSpan value) => OnPropertyChanged(nameof(TempoTotalDisplay));
    partial void OnTempoTotalPecasChanged(TimeSpan value) => OnPropertyChanged(nameof(TempoTotalPecasDisplay));
    partial void OnTempoSessaoAtivaChanged(string value) => OnPropertyChanged(nameof(TempoSessaoAtivaDisplay));
    partial void OnSelectedProjetoChanged(ProjetoDto? value)
    {
        OnPropertyChanged(nameof(HasProjetoContexto));
        if (_suspendSelectedProjetoLoad || value is null)
            return;

        _ = LoadProjetoContextAsync(value.Projeto_id);
    }
    partial void OnProjetoAtivoChanged(ProjetoComRevisoesDto? value)
    {
        OnPropertyChanged(nameof(HasProjetoAtivo));
        OnPropertyChanged(nameof(CanAccessTempoProjeto));
        OnPropertyChanged(nameof(CanAddPeca));
        OnPropertyChanged(nameof(ProjetoAtivoResumoDisplay));
        OnPropertyChanged(nameof(ProjetoAtivoCaminhoDisplay));
        OnPropertyChanged(nameof(RevisoesResumoDisplay));
        OnPropertyChanged(nameof(TempoResumoDisplay));
    }

    /// <summary>
    /// Carrega o molde, o dashboard de ciclo de vida, as pecas e o contexto de projeto associado.
    /// </summary>
    /// <param name="moldeId">Identificador do molde a apresentar.</param>
    /// <returns>Tarefa assincrona da operacao de carregamento.</returns>
    public async Task LoadAsync(int moldeId)
    {
        MoldeId = moldeId;
        ErrorMessage = string.Empty;

        try
        {
            await ExecutePagedLoadAsync(async () =>
            {
                await EnsureCurrentUserAsync();

                var moldeTask = _moldesService.GetByIdAsync(moldeId);
                var dashboardTask = _moldesService.GetDashboardCicloVidaAsync(moldeId);
                var projetosTask = GetAllProjetosAsync(moldeId);

                await Task.WhenAll(moldeTask, dashboardTask, projetosTask);
                var molde = await moldeTask;
                var dashboardCicloVida = await dashboardTask;
                var projetos = await projetosTask;

                if (molde is null)
                {
                    ErrorMessage = "Nao foi possivel carregar o detalhe do molde.";
                    Dashboard = null;
                    Pecas.Clear();
                    Projetos.Clear();
                    Revisoes.Clear();
                    RegistosTempo.Clear();
                    TempoTotalPecas = TimeSpan.Zero;
                    ProjetoAtivo = null;
                    SelectedProjeto = null;
                    ClearFichaTecnica();
                    OnStateCollectionsChanged();
                    return;
                }

                Numero = molde.Numero ?? string.Empty;
                NumeroMoldeCliente = molde.NumeroMoldeCliente ?? string.Empty;
                Nome = molde.Nome ?? string.Empty;
                Descricao = molde.Descricao ?? string.Empty;
                Numero_cavidades = molde.Numero_cavidades;
                TipoPedido = molde.TipoPedido ?? string.Empty;
                ImagemCapaSource = molde.ImagemCapaSource;
                Largura = molde.Largura;
                Comprimento = molde.Comprimento;
                Altura = molde.Altura;
                PesoEstimado = molde.PesoEstimado;
                TipoInjecao = molde.TipoInjecao ?? string.Empty;
                SistemaInjecao = molde.SistemaInjecao ?? string.Empty;
                Contracao = molde.Contracao;
                AcabamentoPeca = molde.AcabamentoPeca ?? string.Empty;
                Cor = molde.Cor;
                MaterialMacho = molde.MaterialMacho ?? string.Empty;
                MaterialCavidade = molde.MaterialCavidade ?? string.Empty;
                MaterialMovimentos = molde.MaterialMovimentos ?? string.Empty;
                MaterialInjecao = molde.MaterialInjecao ?? string.Empty;
                Dashboard = dashboardCicloVida;
                Page = 1;

                await LoadPecasAsync();
                await AtualizarResumoTempoProducaoAsync();

                _suspendSelectedProjetoLoad = true;
                try
                {
                    Projetos.Clear();
                    foreach (var projeto in projetos
                        .OrderByDescending(item => item.Projeto_id)
                        .ThenBy(item => item.NomeProjetoDisplay))
                    {
                        Projetos.Add(projeto);
                    }

                    OnPropertyChanged(nameof(HasProjetos));
                    OnPropertyChanged(nameof(EmptyProjetosMessage));
                    OnPropertyChanged(nameof(EmptyProjetosMessageDisplay));

                    SelectedProjeto = Projetos.Count > 0 ? Projetos[0] : null;
                }
                finally
                {
                    _suspendSelectedProjetoLoad = false;
                }

                if (SelectedProjeto is not null)
                    await LoadProjetoContextAsync(SelectedProjeto.Projeto_id);
                else
                    ClearProjetoContext();
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Dashboard = null;
            Pecas.Clear();
            Projetos.Clear();
            Revisoes.Clear();
            RegistosTempo.Clear();
            TempoTotalPecas = TimeSpan.Zero;
            ProjetoAtivo = null;
            SelectedProjeto = null;
            ClearFichaTecnica();
            OnStateCollectionsChanged();
        }
    }

    /// <summary>
    /// Recarrega a lista de pecas e o respetivo total de tempo acumulado.
    /// </summary>
    /// <returns>Tarefa assincrona da operacao de atualizacao.</returns>
    public async Task RefreshPecasAsync()
    {
        if (MoldeId <= 0)
            return;

        ErrorMessage = string.Empty;

        try
        {
            await ExecutePagedLoadAsync(LoadPecasAsync);
            await AtualizarResumoTempoProducaoAsync();
            if (SelectedProjeto is not null)
                await LoadProjetoContextAsync(SelectedProjeto.Projeto_id);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>
    /// Recarrega o contexto do projeto atualmente selecionado.
    /// </summary>
    /// <returns>Tarefa assincrona da operacao de atualizacao.</returns>
    public async Task RefreshProjetoContextoAsync()
    {
        if (SelectedProjeto is null)
            return;

        await LoadProjetoContextAsync(SelectedProjeto.Projeto_id);
    }

    [RelayCommand]
    private async Task RecarregarProjetoAsync()
    {
        await RefreshProjetoContextoAsync();
    }

    protected override Task LoadPageAsync()
    {
        if (MoldeId <= 0)
            return Task.CompletedTask;

        return ExecutePagedLoadAsync(LoadPecasAsync);
    }

    [RelayCommand]
    private async Task RecarregarAsync()
    {
        await LoadAsync(MoldeId);
    }

    [RelayCommand]
    private async Task GerarPdfAsync()
    {
        if (Dashboard is null)
            return;

        var destinationFolder = await _destinationFolderPickerService.PickFolderAsync("Seleciona a pasta para o PDF do molde");
        if (string.IsNullOrWhiteSpace(destinationFolder))
            return;

        IsGeneratingPdf = true;

        try
        {
            var filePath = await MoldePdfService.GenerateCicloVidaPdfAsync(
                NumeroDisplay,
                NomeDisplay,
                DescricaoDisplay,
                TipoPedidoDisplay,
                Numero_cavidades,
                Dashboard,
                destinationFolder);

            await Launcher.Default.OpenAsync(new OpenFileRequest(
                "Relatorio do ciclo de vida",
                new ReadOnlyFile(filePath)));

            await _dialogService.ShowSuccessAsync(
                "PDF gerado",
                $"O PDF do ciclo de vida foi gerado em {filePath}.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Erro", ex.Message);
        }
        finally
        {
            IsGeneratingPdf = false;
        }
    }

    [RelayCommand]
    private async Task VoltarAsync()
    {
        await _navigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task EditarMoldeAsync()
    {
        if (!IsAdmin || MoldeId <= 0)
            return;

        await _navigationService.GoToAsync($"{nameof(EditarMoldePage)}?molde_id={MoldeId}");
    }

    [RelayCommand]
    private async Task AbrirMoldeAsync(DesenhoMoldeItem? molde)
    {
        if (molde is null || molde.MoldeId <= 0)
            return;

        await _navigationService.GoToAsync($"{nameof(MoldeDetalhePage)}?molde_id={molde.MoldeId}");
    }

    private void ClearFichaTecnica()
    {
        ImagemCapaSource = MoldeImageSourceHelper.FallbackSource;
        Largura = null;
        Comprimento = null;
        Altura = null;
        PesoEstimado = null;
        TipoInjecao = string.Empty;
        SistemaInjecao = string.Empty;
        Contracao = null;
        AcabamentoPeca = string.Empty;
        Cor = null;
        MaterialMacho = string.Empty;
        MaterialCavidade = string.Empty;
        MaterialMovimentos = string.Empty;
        MaterialInjecao = string.Empty;
    }

    private async Task AtualizarResumoTempoProducaoAsync()
    {
        if (MoldeId <= 0)
        {
            TempoTotalPecas = TimeSpan.Zero;
            return;
        }

        try
        {
            var pecasTask = GetAllPecasAsync(MoldeId);
            var registosTask = GetAllRegistosProducaoAsync();

            await Task.WhenAll(pecasTask, registosTask);
            var pecaIds = await pecasTask;
            var registos = await registosTask;

            if (pecaIds.Count == 0 || registos.Count == 0)
            {
                TempoTotalPecas = TimeSpan.Zero;
                return;
            }

            TempoTotalPecas = CalcularTempoTotalPecas(pecaIds, registos);
        }
        catch
        {
            TempoTotalPecas = TimeSpan.Zero;
        }
    }

    private async Task<List<int>> GetAllPecasAsync(int moldeId)
    {
        var primeiraPagina = await _pecasService.GetByMoldeIdAsync(moldeId, 1, 100);
        if (primeiraPagina is null)
            return [];

        var pecas = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _pecasService.GetByMoldeIdAsync(moldeId, page, 100);
            if (pagina?.Items is null)
                continue;

            pecas.AddRange(pagina.Items);
        }

        return pecas
            .Select(item => item.PecaId)
            .Distinct()
            .ToList();
    }

    private async Task<List<RegistoProducaoDto>> GetAllRegistosProducaoAsync()
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

    private static TimeSpan CalcularTempoPeca(IEnumerable<RegistoProducaoDto> registosOrdenados)
    {
        var total = TimeSpan.Zero;
        DateTime? inicioSessao = null;

        foreach (var registo in registosOrdenados)
        {
            var estado = NormalizeEstado(registo.EstadoProducao);

            if (estado is "PREPARACAO" or "EM_CURSO")
            {
                inicioSessao ??= registo.DataHora;
                continue;
            }

            if (estado is EstadoPausado or EstadoConcluido)
            {
                if (inicioSessao.HasValue && registo.DataHora > inicioSessao.Value)
                    total += registo.DataHora - inicioSessao.Value;

                inicioSessao = null;
            }
        }

        if (inicioSessao.HasValue)
            total += DateTime.UtcNow - inicioSessao.Value;

        return total;
    }

    private static TimeSpan CalcularTempoTotalPecas(IEnumerable<int> pecaIds, IEnumerable<RegistoProducaoDto> registos)
    {
        var pecaIdsSet = pecaIds.ToHashSet();
        var total = TimeSpan.Zero;

        foreach (var grupo in registos
                     .Where(item => pecaIdsSet.Contains(item.PecaId))
                     .GroupBy(item => item.PecaId))
        {
            total += CalcularTempoPeca(grupo.OrderBy(item => item.DataHora));
        }

        return total;
    }

    private static string NormalizeEstado(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    private void NotifyFichaTecnicaChanged()
    {
        OnPropertyChanged(nameof(LarguraDisplay));
        OnPropertyChanged(nameof(ComprimentoDisplay));
        OnPropertyChanged(nameof(AlturaDisplay));
        OnPropertyChanged(nameof(PesoEstimadoDisplay));
        OnPropertyChanged(nameof(TipoInjecaoDisplay));
        OnPropertyChanged(nameof(SistemaInjecaoDisplay));
        OnPropertyChanged(nameof(ContracaoDisplay));
        OnPropertyChanged(nameof(AcabamentoPecaDisplay));
        OnPropertyChanged(nameof(CorDisplay));
        OnPropertyChanged(nameof(MaterialMachoDisplay));
        OnPropertyChanged(nameof(MaterialCavidadeDisplay));
        OnPropertyChanged(nameof(MaterialMovimentosDisplay));
        OnPropertyChanged(nameof(MaterialInjecaoDisplay));
    }

    private static string NormalizeTexto(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? ValorNaoDefinido : value.Trim();
    }

    private static string FormatDecimal(decimal? value)
    {
        return value.HasValue
            ? value.Value.ToString("0.##", CultureInfo.CurrentCulture)
            : ValorNaoDefinido;
    }

    [RelayCommand]
    private async Task InserirPecasAsync(DesenhoMoldeItem? molde)
    {
        if (molde is null)
            return;

        var selection = await _dialogService.ShowSelectionAsync(
            $"Inserir pecas para o molde {molde.NumeroMoldeDisplay}",
            DialogCancel,
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
        await _navigationService.GoToAsync($"{nameof(AdicionarPecaPage)}?molde_id={molde.MoldeId}&numero_molde={numeroMolde}");
    }

    [RelayCommand]
    private async Task AdicionarPecaAsync()
    {
        if (MoldeId <= 0 || !CanManagePieces)
            return;

        if (!CanAddPeca)
        {
            await _dialogService.ShowInfoAsync(
                "Pecas bloqueadas",
                "So e possivel adicionar pecas quando o projeto do molde tiver a ultima revisao aprovada pelo cliente.");
            return;
        }

        var numeroMolde = Uri.EscapeDataString(NumeroDisplay);
        await _navigationService.GoToAsync($"{nameof(AdicionarPecaPage)}?molde_id={MoldeId}&numero_molde={numeroMolde}");
    }

    [RelayCommand]
    private async Task EditarPecaAsync(PecaDto? peca)
    {
        if (peca is null || peca.PecaId <= 0 || !CanManagePieces)
            return;

        var numeroMolde = Uri.EscapeDataString(NumeroDisplay);
        await _navigationService.GoToAsync($"{nameof(EditarPecaPage)}?peca_id={peca.PecaId}&numero_molde={numeroMolde}");
    }

    [RelayCommand]
    private async Task ApagarPecaAsync(PecaDto? peca)
    {
        if (peca is null || peca.PecaId <= 0 || !CanManagePieces)
            return;

        var confirmou = await _dialogService.ConfirmDeleteAsync($"a peca {peca.Designacao}");
        if (!confirmou)
            return;

        try
        {
            await _pecasService.DeleteAsync(peca.PecaId);

            await _dialogService.ShowSuccessAsync(
                "Peca removida",
                $"A peca {peca.Designacao} foi eliminada com sucesso.");

            await RefreshPecasAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Eliminar peca", ex.Message);
        }
    }

    [RelayCommand]
    private async Task CriarRevisaoAsync()
    {
        if (SelectedProjeto is null || SelectedProjeto.Projeto_id <= 0 || IsCreatingRevisao)
            return;

        var descricaoAlteracoes = await _dialogService.PromptAsync(
            $"Nova revisao para {SelectedProjeto.NomeProjetoDisplay}",
            "Descreve as alteracoes a validar com o cliente.",
                new PromptDialogOptions
                {
                    Accept = "Criar",
                    Cancel = DialogCancel,
                    Placeholder = "Descreve as alteracoes",
                    MaxLength = 2000,
                    Keyboard = Keyboard.Text
            });

        if (string.IsNullOrWhiteSpace(descricaoAlteracoes))
            return;

        try
        {
            IsCreatingRevisao = true;
            var created = await _revisoesService.CreateAsync(SelectedProjeto.Projeto_id, descricaoAlteracoes.Trim());

            await _dialogService.ShowSuccessAsync(
                "Revisao criada",
                $"A revisao {created?.NumRevisaoDisplay ?? "nova"} foi criada com sucesso.");

            await LoadProjetoContextAsync(SelectedProjeto.Projeto_id);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Revisao", ex.Message);
        }
        finally
        {
            IsCreatingRevisao = false;
        }
    }

    [RelayCommand]
    private async Task ResponderRevisaoAsync(RevisaoDto? revisao)
    {
        if (revisao is null || revisao.Revisao_id <= 0 || IsSendingResposta)
            return;

        if (revisao.Aprovado.HasValue)
        {
            await _dialogService.ShowInfoAsync(
                "Revisao ja respondida",
                "Esta revisao ja tem resposta registada e nao pode ser alterada.");
            return;
        }

        var decisao = await _dialogService.ShowSelectionAsync(
            $"Responder a {revisao.NumRevisaoDisplay}",
            DialogCancel,
            "Aprovar",
            "Rejeitar");

        if (string.IsNullOrWhiteSpace(decisao))
            return;

        var aprovado = string.Equals(decisao, "Aprovar", StringComparison.Ordinal);
        string? feedbackTexto = null;
        string? feedbackImagemPath = null;

        if (!aprovado)
        {
            feedbackTexto = await _dialogService.PromptAsync(
                $"Feedback da {revisao.NumRevisaoDisplay}",
                "Indica o motivo da rejeicao.",
                new PromptDialogOptions
                {
                    Accept = "Guardar",
                    Cancel = DialogCancel,
                    Placeholder = "Feedback do cliente",
                    MaxLength = 4000,
                    Keyboard = Keyboard.Text
                });

            if (string.IsNullOrWhiteSpace(feedbackTexto))
                return;
        }

        try
        {
            IsSendingResposta = true;
            await _revisoesService.UpdateRespostaClienteAsync(revisao.Revisao_id, aprovado, feedbackTexto?.Trim(), feedbackImagemPath);

            await _dialogService.ShowSuccessAsync(
                "Resposta registada",
                $"A resposta da revisao {revisao.NumRevisaoDisplay} foi guardada.");

            if (SelectedProjeto is not null)
                await LoadProjetoContextAsync(SelectedProjeto.Projeto_id);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Resposta da revisao", ex.Message);
        }
        finally
        {
            IsSendingResposta = false;
        }
    }

    [RelayCommand]
    private async Task RegistarTempoAsync()
    {
        if (SelectedProjeto is null || SelectedProjeto.Projeto_id <= 0 || IsRegisteringTempo)
            return;

        if (!_currentUserId.HasValue)
        {
            await _dialogService.ShowErrorAsync(
                "Tempo de desenho",
                "Nao foi possivel identificar o utilizador autenticado.");
            return;
        }

        var estadosDisponiveis = TempoProjetoStateOptions.GetAllowedStates(RegistosTempo);
        if (estadosDisponiveis.Count == 0)
        {
            await _dialogService.ShowInfoAsync(
                "Tempo de desenho",
                "Este projeto ja terminou e nao permite novos registos de tempo.");
            return;
        }

        var estado = await _dialogService.ShowSelectionAsync(
            $"Registar tempo para {SelectedProjeto.NomeProjetoDisplay}",
            DialogCancel,
            estadosDisponiveis.ToArray());

        if (string.IsNullOrWhiteSpace(estado))
            return;

        try
        {
            IsRegisteringTempo = true;
            await _registosTempoProjetoService.CreateAsync(SelectedProjeto.Projeto_id, _currentUserId.Value, estado);

            await _dialogService.ShowSuccessAsync(
                "Tempo registado",
                $"O estado {estado} foi registado no projeto {SelectedProjeto.NomeProjetoDisplay}.");

            await LoadProjetoContextAsync(SelectedProjeto.Projeto_id);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Tempo de desenho", ex.Message);
        }
        finally
        {
            IsRegisteringTempo = false;
        }
    }

    private async Task EnsureCurrentUserAsync()
    {
        if (_currentUserId.HasValue)
            return;

        _currentUserId = _sessaoPersistidaService.TryGetCurrentUserId();
        await EnsureUserRoleAsync();
    }

    private async Task EnsureUserRoleAsync()
    {
        if (_roleLoaded)
            return;

        _roleLoaded = true;

        try
        {
            await _authorizationService.GetCurrentRoleAsync();
            IsAdmin = _authorizationService.CanCreateMachines();
            CanManagePieces = _authorizationService.CanManagePieces();
        }
        catch
        {
            IsAdmin = false;
            CanManagePieces = false;
        }
    }

    private async Task LoadProjetoContextAsync(int projetoId)
    {
        if (projetoId <= 0)
        {
            ClearProjetoContext();
            return;
        }

        IsLoadingProjeto = true;

        try
        {
            await EnsureCurrentUserAsync();

            var projetoTask = _projetosService.GetWithRevisoesAsync(projetoId);
            Task<List<RegistoTempoProjetoDto>>? historicoTask = null;

            if (CanManagePieces && _currentUserId.HasValue)
                historicoTask = GetAllRegistosTempoAsync(projetoId, _currentUserId.Value);

            if (historicoTask is null)
            {
                ProjetoAtivo = await projetoTask;
            }
            else
            {
                await Task.WhenAll(projetoTask, historicoTask);
                ProjetoAtivo = await projetoTask;
            }

            Revisoes.Clear();
            if (ProjetoAtivo?.Revisoes is not null)
            {
                foreach (var revisao in ProjetoAtivo.Revisoes.OrderByDescending(item => item.NumRevisao))
                    Revisoes.Add(revisao);
            }

            RegistosTempo.Clear();
            var historico = historicoTask is null ? [] : await historicoTask;
            foreach (var registo in historico.OrderByDescending(item => item.Data_hora))
            {
                RegistosTempo.Add(registo);
            }

            AtualizarResumoTempo();
            OnStateCollectionsChanged();
        }
        catch
        {
            ClearProjetoContext();
        }
        finally
        {
            IsLoadingProjeto = false;
        }
    }

    private async Task<List<RegistoTempoProjetoDto>> GetAllRegistosTempoAsync(int projetoId, int autorId)
    {
        var primeiraPagina = await _registosTempoProjetoService.GetHistoricoAsync(projetoId, autorId, 1, 100);
        if (primeiraPagina is null)
            return [];

        var registos = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _registosTempoProjetoService.GetHistoricoAsync(projetoId, autorId, page, 100);
            if (pagina?.Items is null)
                continue;

            registos.AddRange(pagina.Items);
        }

        return registos;
    }

    private async Task<List<ProjetoDto>> GetAllProjetosAsync(int moldeId)
    {
        var primeiraPagina = await _projetosService.GetByMoldeIdAsync(moldeId, 1, 100);
        if (primeiraPagina is null)
            return [];

        var projetos = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _projetosService.GetByMoldeIdAsync(moldeId, page, 100);
            if (pagina?.Items is null)
                continue;

            projetos.AddRange(pagina.Items);
        }

        return projetos;
    }

    private void AtualizarResumoTempo()
    {
        if (RegistosTempo.Count == 0)
        {
            TempoRegistadoTotal = TimeSpan.Zero;
            TempoSessaoAtiva = string.Empty;
            return;
        }

        var registosOrdenados = RegistosTempo
            .OrderBy(item => item.Data_hora)
            .ToList();

        var total = TimeSpan.Zero;
        DateTime? inicioSessao = null;

        foreach (var registo in registosOrdenados)
        {
            var estado = registo.Estado_tempo.Trim().ToUpperInvariant();

            if (estado is "INICIADO" or "RETOMADO")
            {
                inicioSessao ??= registo.Data_hora;
                continue;
            }

            if (estado is EstadoPausado or EstadoConcluido)
            {
                if (inicioSessao.HasValue && registo.Data_hora > inicioSessao.Value)
                    total += registo.Data_hora - inicioSessao.Value;

                inicioSessao = null;
            }
        }

        if (inicioSessao.HasValue)
            total += DateTime.UtcNow - inicioSessao.Value;

        TempoRegistadoTotal = total;
        TempoSessaoAtiva = inicioSessao.HasValue
            ? $"Sessao ativa desde {inicioSessao.Value.ToLocalTime():dd/MM/yyyy HH:mm}"
            : "Sem sessao ativa";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return "0m";

        var totalHours = (int)duration.TotalHours;
        var minutes = duration.Minutes;

        if (totalHours <= 0)
            return $"{minutes}m";

        return minutes <= 0 ? $"{totalHours}h" : $"{totalHours}h {minutes:00}m";
    }

    private void ClearProjetoContext()
    {
        ProjetoAtivo = null;
        Revisoes.Clear();
        RegistosTempo.Clear();
        TempoRegistadoTotal = TimeSpan.Zero;
        TempoSessaoAtiva = string.Empty;
        OnStateCollectionsChanged();
    }

    private void OnStateCollectionsChanged()
    {
        OnPropertyChanged(nameof(HasMoldes));
        OnPropertyChanged(nameof(HasPecas));
        OnPropertyChanged(nameof(HasProjetos));
        OnPropertyChanged(nameof(HasRevisoes));
        OnPropertyChanged(nameof(HasRegistosTempo));
        OnPropertyChanged(nameof(HasProjetoAtivo));
        OnPropertyChanged(nameof(CanAddPeca));
        OnPropertyChanged(nameof(EmptyProjetosMessage));
        OnPropertyChanged(nameof(EmptyProjetosMessageDisplay));
        OnPropertyChanged(nameof(EmptyRevisoesMessage));
        OnPropertyChanged(nameof(EmptyTempoMessage));
        OnPropertyChanged(nameof(ProjetoSectionDescription));
        OnPropertyChanged(nameof(ProjetoAtivoResumoDisplay));
        OnPropertyChanged(nameof(ProjetoAtivoCaminhoDisplay));
        OnPropertyChanged(nameof(RevisoesResumoDisplay));
        OnPropertyChanged(nameof(TempoResumoDisplay));
        OnPropertyChanged(nameof(TempoTotalDisplay));
        OnPropertyChanged(nameof(TempoTotalPecasDisplay));
        OnPropertyChanged(nameof(TempoSessaoAtivaDisplay));
    }

    private string BuildDistribuicaoDisplay(int value)
    {
        if (DistribuicaoTotal <= 0)
            return $"{value} pecas (0%)";

        var percentagem = (decimal)value / DistribuicaoTotal * 100m;
        return $"{value} pecas ({percentagem:0.#}%)";
    }

    private static bool HasOpenOrApprovedRevision(ProjetoComRevisoesDto? projeto)
    {
        var ultimaRevisao = projeto?.Revisoes
            .OrderByDescending(item => item.NumRevisao)
            .FirstOrDefault();

        return ultimaRevisao is not null
            && (!ultimaRevisao.DataResposta.HasValue || ultimaRevisao.Aprovado == true);
    }

    private async Task LoadPecasAsync()
    {
        var pagina = await _pecasService.GetByMoldeIdAsync(MoldeId, Page, PageSize);
        if (pagina is null)
        {
            Pecas.Clear();
            UpdatePagination(0, 1);
            OnPropertyChanged(nameof(HasPecas));
            return;
        }

        UpdatePagination(pagina.TotalItems, pagina.TotalPages);

        Pecas.Clear();
        foreach (var peca in pagina.Items.OrderBy(item => item.Prioridade).ThenBy(item => item.NumeroPeca).ThenBy(item => item.Designacao))
            Pecas.Add(peca);

        OnPropertyChanged(nameof(HasPecas));
    }

    private async Task ImportarPecasCsvAsync(DesenhoMoldeItem molde)
    {
        try
        {
            var file = await _filePickerService.PickAsync(new PickOptions
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

            await RefreshPecasAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "Importacao de pecas",
                ex.Message);
        }
    }

    private static readonly FilePickerFileType CsvFileTypes = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.WinUI] = [".csv"],
        [DevicePlatform.Android] = ["text/csv", "text/comma-separated-values", "application/csv", "application/vnd.ms-excel"],
        [DevicePlatform.iOS] = ["public.comma-separated-values-text", "public.plain-text"],
        [DevicePlatform.MacCatalyst] = ["public.comma-separated-values-text", "public.plain-text"]
    });
}
