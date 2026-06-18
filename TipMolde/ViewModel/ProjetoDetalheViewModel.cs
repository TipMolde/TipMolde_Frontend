using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel.Helpers;

namespace TipMolde.ViewModel;

public partial class ProjetoDetalheViewModel : ObservableObject
{
    private readonly ProjetosService _projetosService;
    private readonly RevisoesService _revisoesService;
    private readonly RegistosTempoProjetoService _registosTempoProjetoService;
    private readonly AuthorizationService _authorizationService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly IDialogService _dialogService;
    private bool _roleLoaded;
    private int? _currentUserId;

    public ProjetoDetalheViewModel(
        ProjetosService projetosService,
        RevisoesService revisoesService,
        RegistosTempoProjetoService registosTempoProjetoService,
        AuthorizationService authorizationService,
        SessaoPersistidaService sessaoPersistidaService,
        IDialogService dialogService)
    {
        _projetosService = projetosService;
        _revisoesService = revisoesService;
        _registosTempoProjetoService = registosTempoProjetoService;
        _authorizationService = authorizationService;
        _sessaoPersistidaService = sessaoPersistidaService;
        _dialogService = dialogService;
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

    public async Task LoadAsync(int projetoId)
    {
        ProjetoId = projetoId;
        ErrorMessage = string.Empty;

        try
        {
            await ExecuteLoadAsync(async () =>
            {
                await EnsureCurrentUserAsync();

                var projeto = await _projetosService.GetWithRevisoesAsync(projetoId);
                if (projeto is null)
                {
                    ErrorMessage = "Nao foi possivel carregar o projeto.";
                    ClearContext();
                    return;
                }

                Projeto = projeto;

                Revisoes.Clear();
                foreach (var revisao in projeto.Revisoes.OrderByDescending(item => item.NumRevisao))
                    Revisoes.Add(revisao);

                RegistosTempo.Clear();
                if (CanAccessTempo && _currentUserId.HasValue)
                {
                    var historico = await GetAllRegistosTempoAsync(projetoId, _currentUserId.Value);
                    foreach (var registo in historico.OrderByDescending(item => item.Data_hora))
                        RegistosTempo.Add(registo);
                }

                AtualizarResumoTempo();
                OnStateChanged();
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ClearContext();
        }
    }

    [RelayCommand]
    private async Task RecarregarAsync()
    {
        if (ProjetoId <= 0)
            return;

        await LoadAsync(ProjetoId);
    }

    [RelayCommand]
    private async Task VoltarAsync()
    {
        await ShellNavigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task CriarRevisaoAsync()
    {
        if (!IsAdmin || Projeto is null || Projeto.Projeto_id <= 0 || isCreatingRevisao)
            return;

        if (!CanCreateRevisao)
        {
            await _dialogService.ShowInfoAsync(
                "Revisao bloqueada",
                GetBloqueioCriacaoRevisaoMensagem());
            return;
        }

        var descricaoAlteracoes = await _dialogService.PromptAsync(
            $"Nova revisao para {Projeto.NomeProjetoDisplay}",
            "Descreve as alteracoes a validar com o cliente.",
            new PromptDialogOptions
            {
                Accept = "Criar",
                Cancel = "Cancelar",
                Placeholder = "Descreve as alteracoes",
                MaxLength = 2000,
                Keyboard = Keyboard.Text
            });

        if (string.IsNullOrWhiteSpace(descricaoAlteracoes))
            return;

        try
        {
            IsCreatingRevisao = true;
            var created = await _revisoesService.CreateAsync(Projeto.Projeto_id, descricaoAlteracoes.Trim());

            await _dialogService.ShowSuccessAsync(
                "Revisao criada",
                $"A revisao {created?.NumRevisaoDisplay ?? "nova"} foi criada com sucesso.");

            await LoadAsync(Projeto.Projeto_id);
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
        if (!IsAdmin || revisao is null || revisao.Revisao_id <= 0 || isSendingResposta)
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
            "Cancelar",
            "Aprovar",
            "Rejeitar");

        if (string.IsNullOrWhiteSpace(decisao))
            return;

        var aprovado = string.Equals(decisao, "Aprovar", StringComparison.Ordinal);
        string? feedbackTexto = null;
        string? attachmentPath = null;

        if (!aprovado)
        {
            feedbackTexto = await _dialogService.PromptAsync(
                $"Feedback da {revisao.NumRevisaoDisplay}",
                "Indica o motivo da rejeicao.",
                new PromptDialogOptions
                {
                    Accept = "Guardar",
                    Cancel = "Cancelar",
                    Placeholder = "Feedback do cliente",
                    MaxLength = 4000,
                    Keyboard = Keyboard.Text
                });

            if (string.IsNullOrWhiteSpace(feedbackTexto))
                return;

            var adicionarAnexo = await _dialogService.ShowSelectionAsync(
                "Anexo da revisao",
                "Sem anexo",
                "Anexar ficheiro");

            if (string.Equals(adicionarAnexo, "Anexar ficheiro", StringComparison.Ordinal))
                attachmentPath = await SelecionarAnexoAsync();
        }

        try
        {
            IsSendingResposta = true;
            if (string.IsNullOrWhiteSpace(attachmentPath))
            {
                await _revisoesService.UpdateRespostaClienteAsync(
                    revisao.Revisao_id,
                    aprovado,
                    feedbackTexto?.Trim(),
                    null);
            }
            else
            {
                await _revisoesService.UpdateRespostaClienteComAnexoAsync(
                    revisao.Revisao_id,
                    aprovado,
                    feedbackTexto?.Trim(),
                    attachmentPath);
            }

            await _dialogService.ShowSuccessAsync(
                "Resposta registada",
                $"A resposta da revisao {revisao.NumRevisaoDisplay} foi guardada.");

            await LoadAsync(ProjetoId);
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
    private async Task AbrirAnexoAsync(RevisaoDto? revisao)
    {
        if (revisao is null || string.IsNullOrWhiteSpace(revisao.FeedbackImagemPath))
            return;

        try
        {
            var localPath = await _revisoesService.DownloadAnexoAsync(revisao.FeedbackImagemPath);
            await _dialogService.ShowSuccessAsync(
                "Anexo descarregado",
                $"O ficheiro foi guardado em {localPath}.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Anexo da revisao", ex.Message);
        }
    }

    [RelayCommand]
    private async Task RegistarTempoAsync()
    {
        if (!CanAccessTempo || Projeto is null || Projeto.Projeto_id <= 0 || isRegisteringTempo)
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
                "Nao foi possivel determinar estados disponiveis para este projeto.");
            return;
        }

        var estado = await _dialogService.ShowSelectionAsync(
            $"Registar tempo para {Projeto.NomeProjetoDisplay}",
            "Cancelar",
            estadosDisponiveis.ToArray());

        if (string.IsNullOrWhiteSpace(estado))
            return;

        try
        {
            IsRegisteringTempo = true;
            await _registosTempoProjetoService.CreateAsync(Projeto.Projeto_id, _currentUserId.Value, estado);

            await _dialogService.ShowSuccessAsync(
                "Tempo registado",
                $"O estado {estado} foi registado no projeto {Projeto.NomeProjetoDisplay}.");

            await LoadAsync(Projeto.Projeto_id);
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

    private async Task ExecuteLoadAsync(Func<Task> loadAction)
    {
        if (IsLoading)
            return;

        IsLoading = true;

        try
        {
            await loadAction();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<string?> SelecionarAnexoAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Seleciona imagem ou documento para a revisao",
                FileTypes = RevisaoAnexoFileTypes
            });

            return file?.FullPath;
        }
        catch
        {
            return null;
        }
    }

    private static bool HasOpenOrApprovedRevision(ProjetoComRevisoesDto? projeto)
    {
        var ultimaRevisao = projeto?.Revisoes
            .OrderByDescending(item => item.NumRevisao)
            .FirstOrDefault();

        return ultimaRevisao is not null
            && (!ultimaRevisao.DataResposta.HasValue || ultimaRevisao.Aprovado == true);
    }

    private string GetBloqueioCriacaoRevisaoMensagem()
    {
        if (Projeto is null)
            return "Nao foi possivel carregar o projeto.";

        var ultimaRevisao = Projeto.Revisoes
            .OrderByDescending(item => item.NumRevisao)
            .FirstOrDefault();

        if (ultimaRevisao is null)
            return "Nao existe historico de revisoes suficiente para criar uma nova revisao.";

        if (!ultimaRevisao.DataResposta.HasValue)
            return "Ja existe uma revisao em aberto para este projeto.";

        if (ultimaRevisao.Aprovado == true)
            return "O cliente ja aprovou a ultima revisao, por isso nao e possivel criar outra.";

        return "Nao e possivel criar uma nova revisao neste momento.";
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
            CanManageTempo = _authorizationService.CanManagePieces();
        }
        catch
        {
            IsAdmin = false;
            CanManageTempo = false;
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

    private void AtualizarResumoTempo()
    {
        if (RegistosTempo.Count == 0)
        {
            TempoRegistadoTotal = TimeSpan.Zero;
            TempoSessaoAtiva = string.Empty;
            return;
        }

        var registosOrdenados = RegistosTempo.OrderBy(item => item.Data_hora).ToList();
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

            if (estado is "PAUSADO" or "CONCLUIDO")
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

    private void ClearContext()
    {
        Projeto = null;
        Revisoes.Clear();
        RegistosTempo.Clear();
        TempoRegistadoTotal = TimeSpan.Zero;
        TempoSessaoAtiva = string.Empty;
        OnStateChanged();
    }

    private void OnStateChanged()
    {
        OnPropertyChanged(nameof(HasProjeto));
        OnPropertyChanged(nameof(HasRevisoes));
        OnPropertyChanged(nameof(HasRegistosTempo));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(CanAccessTempo));
        OnPropertyChanged(nameof(CanCreateRevisao));
        OnPropertyChanged(nameof(ProjetoTituloDisplay));
        OnPropertyChanged(nameof(ProjetoSubtituloDisplay));
        OnPropertyChanged(nameof(ProjetoCaminhoDisplay));
        OnPropertyChanged(nameof(RevisoesResumoDisplay));
        OnPropertyChanged(nameof(EmptyRevisoesMessage));
        OnPropertyChanged(nameof(EmptyTempoMessage));
        OnPropertyChanged(nameof(TempoTotalDisplay));
        OnPropertyChanged(nameof(TempoSessaoAtivaDisplay));
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

    private static readonly FilePickerFileType RevisaoAnexoFileTypes = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.WinUI] = [".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg"],
        [DevicePlatform.Android] = ["application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "image/png", "image/jpeg"],
        [DevicePlatform.iOS] = ["com.adobe.pdf", "org.openxmlformats.wordprocessingml.document", "public.jpeg", "public.png"],
        [DevicePlatform.MacCatalyst] = ["com.adobe.pdf", "org.openxmlformats.wordprocessingml.document", "public.jpeg", "public.png"]
    });
}
