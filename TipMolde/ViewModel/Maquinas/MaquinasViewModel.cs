using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere a listagem, criacao e edicao operacional de maquinas e fases dedicadas.
/// </summary>
public partial class MaquinasViewModel : SearchableViewModel
{
    private const string SuccessTitle = "Sucesso";
    private const string EstadoDisponivel = "DISPONIVEL";
    private const string EstadoEmUso = "EM_USO";
    private const string EstadoManutencao = "MANUTENCAO";
    private const string EstadoDisponivelDisplay = "Disponivel";
    private const string EstadoEmUsoDisplay = "Em Uso";
    private const string EstadoManutencaoDisplay = "Manutencao";

    private readonly MaquinasService _maquinasService;
    private readonly FasesProducaoService _fasesProducaoService;
    private readonly AuthorizationService _authorizationService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly Dictionary<int, string> _fasesPorId = [];
    private readonly List<MaquinaItem> _todasMaquinas = [];
    private readonly List<MaquinaItem> _maquinasFiltradas = [];
    private string _loadedSearchTerm = string.Empty;
    private readonly AsyncRelayCommand _pesquisarCommand;
    private readonly AsyncRelayCommand _limparPesquisaCommand;
    private bool _fasesLoaded;
    private bool _maquinasLoaded;
    private bool _permissionsLoaded;

    /// <summary>
    /// Construtor do view model da pagina de maquinas.
    /// </summary>
    /// <param name="maquinasService">Servico usado para consultar e alterar maquinas.</param>
    /// <param name="fasesProducaoService">Servico usado para obter as fases de producao disponiveis.</param>
    /// <param name="authorizationService">Servico usado para validar permissoes funcionais.</param>
    /// <param name="dialogService">Servico usado para apresentar confirmacoes e mensagens de erro.</param>
    /// <param name="navigationService">Servico usado para navegacao programatica entre paginas.</param>
    public MaquinasViewModel(
        MaquinasService maquinasService,
        FasesProducaoService fasesProducaoService,
        AuthorizationService authorizationService,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        _maquinasService = maquinasService;
        _fasesProducaoService = fasesProducaoService;
        _authorizationService = authorizationService;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _pesquisarCommand = new AsyncRelayCommand(PesquisarAsync);
        _limparPesquisaCommand = new AsyncRelayCommand(LimparPesquisaAsync, () => HasSearch);
        PageSize = 5;

        PropertyChanged += (_, e) =>
        {
            if (string.Equals(e.PropertyName, nameof(SearchTerm), StringComparison.Ordinal))
                _limparPesquisaCommand.NotifyCanExecuteChanged();

            if (string.Equals(e.PropertyName, nameof(IsLoading), StringComparison.Ordinal))
            {
                NotifyCanAdicionarMaquinaChanged();
                NotifyCanAdicionarFaseChanged();
                NotifyCanGuardarEdicaoChanged();
            }
        };
    }

    public ObservableCollection<MaquinaItem> Maquinas { get; } = new();
    public ObservableCollection<EstadoMaquinaOption> EstadoMaquinaOptions { get; } = new();
    public ObservableCollection<EstadoMaquinaOption> EditEstadoMaquinaOptions { get; } = new();
    public ObservableCollection<FaseDedicadaOption> FasesDedicadas { get; } = new();
    public ObservableCollection<FaseProducaoItem> FasesProducao { get; } = new();
    public ObservableCollection<FaseNomeOption> FaseNomeOptions { get; } = new();

    public new IAsyncRelayCommand PesquisarCommand => _pesquisarCommand;
    public new IAsyncRelayCommand LimparPesquisaCommand => _limparPesquisaCommand;

    [ObservableProperty]
    private bool isAddMaquinaVisible;

    [ObservableProperty]
    private bool isFasesProducaoVisible;

    [ObservableProperty]
    private bool isEditMaquinaVisible;

    [ObservableProperty]
    private string novoMaquinaId = string.Empty;

    [ObservableProperty]
    private string novoNumero = string.Empty;

    [ObservableProperty]
    private string novoNomeModelo = string.Empty;

    [ObservableProperty]
    private string novoIpAddress = string.Empty;

    [ObservableProperty]
    private EstadoMaquinaOption? selectedEstadoMaquinaOption;

    [ObservableProperty]
    private FaseDedicadaOption? selectedFaseDedicadaOption;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private bool isSavingEdicao;

    [ObservableProperty]
    private MaquinaItem? maquinaEmEdicao;

    [ObservableProperty]
    private string editarIpAddress = string.Empty;

    [ObservableProperty]
    private EstadoMaquinaOption? selectedEditEstadoMaquinaOption;

    [ObservableProperty]
    private FaseNomeOption? selectedFaseNomeOption;

    [ObservableProperty]
    private string novaDescricaoFase = string.Empty;

    [ObservableProperty]
    private bool isSavingFase;

    [ObservableProperty]
    private string fasesErrorMessage = string.Empty;

    [ObservableProperty]
    private bool canCreateMachine;

    [ObservableProperty]
    private bool canDeleteMachine;

    [ObservableProperty]
    private bool canManageProductionPhases;

    [ObservableProperty]
    private bool canEditMachine;

    public bool HasMaquinas => Maquinas.Count > 0;
    public bool HasFasesProducao => FasesProducao.Count > 0;
    public bool HasMachineManagementShortcuts => CanCreateMachine || CanManageProductionPhases;
    public string EmptyMessage => string.IsNullOrWhiteSpace(_loadedSearchTerm)
        ? "Não existem máquinas registadas para apresentar."
        : "Nenhuma maquina corresponde aos filtros atuais.";
    public string EmptyFasesMessage => "Não existem fases de produção registadas.";
    public int TotalMaquinasDisponiveis => _maquinasFiltradas.Count(item => item.Disponivel);
    public int TotalMaquinasEmUso => _maquinasFiltradas.Count(item => item.EmUtilizacao);
    public int TotalMaquinasManutencao => _maquinasFiltradas.Count(item => item.EmManutencao);
    public int TotalMaquinasComConexao => _maquinasFiltradas.Count(item => item.HasIpAddress);
    public bool CanAdicionarMaquina => !IsLoading
                                       && CanCreateMachine
                                       && !IsSaving
                                       && !string.IsNullOrWhiteSpace(NovoMaquinaId)
                                       && !string.IsNullOrWhiteSpace(NovoNumero)
                                       && !string.IsNullOrWhiteSpace(NovoNomeModelo)
                                       && SelectedEstadoMaquinaOption is not null
                                       && SelectedFaseDedicadaOption is not null;
    public bool HasFasesError => !string.IsNullOrWhiteSpace(FasesErrorMessage);
    public bool CanAdicionarFase => !IsLoading
                                    && CanManageProductionPhases
                                    && !IsSavingFase
                                    && SelectedFaseNomeOption is not null;
    public bool CanGuardarEdicao => !IsLoading
                                    && !IsSavingEdicao
                                    && MaquinaEmEdicao is not null
                                    && SelectedEditEstadoMaquinaOption is not null
                                    && HasMudancasEdicao();
    public string MaquinaEmEdicaoDisplay => MaquinaEmEdicao is null
        ? "Sem máquina selecionada."
        : $"{MaquinaEmEdicao.NumeroDisplay} - {MaquinaEmEdicao.NomeModeloDisplay}";
    public string EstadoAtualEdicaoDisplay => MaquinaEmEdicao?.EstadoDisplay ?? "Sem estado";
    public string TransicoesPermitidasDisplay => BuildTransicoesPermitidasDisplay();

    partial void OnNovoMaquinaIdChanged(string value) => NotifyCanAdicionarMaquinaChanged();

    partial void OnNovoNumeroChanged(string value) => NotifyCanAdicionarMaquinaChanged();

    partial void OnNovoNomeModeloChanged(string value) => NotifyCanAdicionarMaquinaChanged();

    partial void OnSelectedEstadoMaquinaOptionChanged(EstadoMaquinaOption? value) => NotifyCanAdicionarMaquinaChanged();

    partial void OnSelectedFaseDedicadaOptionChanged(FaseDedicadaOption? value) => NotifyCanAdicionarMaquinaChanged();

    partial void OnIsSavingChanged(bool value) => NotifyCanAdicionarMaquinaChanged();

    partial void OnIsSavingEdicaoChanged(bool value) => NotifyCanGuardarEdicaoChanged();

    partial void OnSelectedFaseNomeOptionChanged(FaseNomeOption? value)
    {
        FasesErrorMessage = string.Empty;
        NotifyCanAdicionarFaseChanged();
    }

    partial void OnNovaDescricaoFaseChanged(string value)
    {
        FasesErrorMessage = string.Empty;
    }

    partial void OnIsSavingFaseChanged(bool value) => NotifyCanAdicionarFaseChanged();

    partial void OnFasesErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasFasesError));
    }

    partial void OnCanCreateMachineChanged(bool value)
    {
        OnPropertyChanged(nameof(HasMachineManagementShortcuts));
        NotifyCanAdicionarMaquinaChanged();
    }

    partial void OnCanManageProductionPhasesChanged(bool value)
    {
        OnPropertyChanged(nameof(HasMachineManagementShortcuts));
        NotifyCanAdicionarFaseChanged();

        if (!value)
            IsFasesProducaoVisible = false;
    }

    partial void OnMaquinaEmEdicaoChanged(MaquinaItem? value)
    {
        OnPropertyChanged(nameof(MaquinaEmEdicaoDisplay));
        OnPropertyChanged(nameof(EstadoAtualEdicaoDisplay));
        OnPropertyChanged(nameof(TransicoesPermitidasDisplay));
        NotifyCanGuardarEdicaoChanged();
    }

    partial void OnEditarIpAddressChanged(string value) => NotifyCanGuardarEdicaoChanged();

    partial void OnSelectedEditEstadoMaquinaOptionChanged(EstadoMaquinaOption? value) => NotifyCanGuardarEdicaoChanged();

    /// <summary>
    /// Carrega permissoes, fases e a listagem de maquinas para apresentacao inicial.
    /// </summary>
    /// <returns>Tarefa assincrona da operacao de carregamento.</returns>
    public async Task LoadAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            await EnsurePermissionsLoadedAsync();
            await EnsureFasesLoadedAsync();
            await RefreshMaquinasAsync(forceReload: true, resetToFirstPage: true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Maquinas.Clear();
            UpdatePagination(0, 1);
            NotifyCollectionStateChanged();
        }
    }

    protected override async Task LoadPageAsync()
    {
        await RefreshMaquinasAsync(forceReload: false, resetToFirstPage: false);
    }

    [RelayCommand]
    private async Task RecarregarAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            await EnsureFasesLoadedAsync(forceReload: true);
            await RefreshMaquinasAsync(forceReload: true, resetToFirstPage: true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Maquinas.Clear();
            UpdatePagination(0, 1);
            NotifyCollectionStateChanged();
        }
    }

    [RelayCommand]
    private void ToggleAdicionarMaquina()
    {
        if (!CanCreateMachine)
            return;

        IsAddMaquinaVisible = !IsAddMaquinaVisible;
        if (IsAddMaquinaVisible)
        {
            IsFasesProducaoVisible = false;
            IsEditMaquinaVisible = false;
        }

        if (IsAddMaquinaVisible)
            LoadFormDefaults();
    }

    [RelayCommand]
    private async Task ToggleFasesProducaoAsync()
    {
        if (!CanManageProductionPhases)
            return;

        IsFasesProducaoVisible = !IsFasesProducaoVisible;

        if (IsFasesProducaoVisible)
        {
            IsAddMaquinaVisible = false;
            IsEditMaquinaVisible = false;
            FasesErrorMessage = string.Empty;
            await EnsureFasesLoadedAsync(forceReload: true);
        }
    }

    [RelayCommand]
    private void FecharFormularioAdicionar()
    {
        IsAddMaquinaVisible = false;
        ResetFormulario();
    }

    [RelayCommand]
    private void FecharFormularioEditar()
    {
        IsEditMaquinaVisible = false;
        ResetFormularioEdicao();
    }

    [RelayCommand(CanExecute = nameof(CanAdicionarFase))]
    private async Task ConfirmarAdicionarFaseAsync()
    {
        if (!CanManageProductionPhases)
            return;

        if (SelectedFaseNomeOption is null)
        {
            FasesErrorMessage = "Selecione o nome da fase de produção.";
            return;
        }

        IsSavingFase = true;
        FasesErrorMessage = string.Empty;

        try
        {
            await _fasesProducaoService.CreateAsync(
                SelectedFaseNomeOption.Value,
                NormalizeOptional(NovaDescricaoFase));

            await _dialogService.ShowSuccessAsync(
                SuccessTitle,
                $"A fase de produção {SelectedFaseNomeOption.DisplayName} foi criada com sucesso.");

            ResetFormularioFase();
            await EnsureFasesLoadedAsync(forceReload: true);
        }
        catch (Exception ex)
        {
            FasesErrorMessage = ex.Message;
        }
        finally
        {
            IsSavingFase = false;
        }
    }

    [RelayCommand]
    private async Task EliminarFaseAsync(FaseProducaoItem? fase)
    {
        if (!CanManageProductionPhases)
            return;

        if (fase is null || fase.FasesProducao_id <= 0)
            return;

        var confirmar = await _dialogService.ConfirmDeleteAsync($"a fase de produção {fase.NomeDisplay}");
        if (!confirmar)
            return;

        FasesErrorMessage = string.Empty;

        try
        {
            await _fasesProducaoService.DeleteAsync(fase.FasesProducao_id);

            await _dialogService.ShowSuccessAsync(
                SuccessTitle,
                $"A fase de produção {fase.NomeDisplay} foi eliminada com sucesso.");

            await EnsureFasesLoadedAsync(forceReload: true);
        }
        catch (Exception ex)
        {
            FasesErrorMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAdicionarMaquina))]
    private async Task ConfirmarAdicionarMaquinaAsync()
    {
        if (!CanCreateMachine)
            return;

        var validationMessage = BuildValidationMessage();
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            ErrorMessage = validationMessage;
            return;
        }

        IsSaving = true;
        ErrorMessage = string.Empty;

        try
        {
            await _maquinasService.CreateAsync(
                maquinaId: int.Parse(NovoMaquinaId.Trim()),
                numero: int.Parse(NovoNumero.Trim()),
                nomeModelo: NovoNomeModelo.Trim(),
                ipAddress: NormalizeOptional(NovoIpAddress),
                estado: SelectedEstadoMaquinaOption!.Value,
                faseDedicadaId: SelectedFaseDedicadaOption!.Id);

            await _dialogService.ShowSuccessAsync(
                SuccessTitle,
                $"A máquina {NovoNumero.Trim()} foi criada com sucesso.");

            IsAddMaquinaVisible = false;
            ResetFormulario();
            await EnsureFasesLoadedAsync(forceReload: true);
            await RefreshMaquinasAsync(forceReload: true, resetToFirstPage: true);
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

    [RelayCommand]
    private async Task AbrirDetalhesAsync(MaquinaItem? maquina)
    {
        if (maquina is null || !maquina.HasIpAddress)
            return;

        await _navigationService.GoToAsync(nameof(MaquinaDetalhePage), new Dictionary<string, object>
        {
            ["maquina_id"] = maquina.Maquina_id
        });
    }

    [RelayCommand]
    private async Task EditarMaquinaAsync(MaquinaItem? maquina)
    {
        if (!CanEditMachine)
            return;

        if (maquina is null)
            return;

        await _navigationService.GoToAsync(nameof(EditarMaquinaPage), new Dictionary<string, object>
        {
            ["maquina_id"] = maquina.Maquina_id,
            ["numero"] = maquina.Numero,
            ["nome_modelo"] = maquina.NomeModelo,
            ["fase_dedicada"] = maquina.FaseDedicadaDisplay,
            ["estado_atual"] = maquina.Estado,
            ["ip_address"] = maquina.IpAddress
        });
    }

    [RelayCommand(CanExecute = nameof(CanGuardarEdicao))]
    private async Task GuardarEdicaoMaquinaAsync()
    {
        if (MaquinaEmEdicao is null || SelectedEditEstadoMaquinaOption is null || !HasMudancasEdicao())
            return;

        IsSavingEdicao = true;
        ErrorMessage = string.Empty;

        try
        {
            var novoIp = NormalizeOptional(EditarIpAddress);
            var ipAlterado = !string.Equals(
                NormalizeOptional(MaquinaEmEdicao.IpAddress),
                novoIp,
                StringComparison.OrdinalIgnoreCase);

            var estadoAlterado = !string.Equals(
                MaquinaEmEdicao.Estado,
                SelectedEditEstadoMaquinaOption.Value,
                StringComparison.OrdinalIgnoreCase);

            await _maquinasService.UpdateAsync(
                MaquinaEmEdicao.Maquina_id,
                ipAddress: ipAlterado ? novoIp : null,
                estado: estadoAlterado ? SelectedEditEstadoMaquinaOption.Value : null);

            await _dialogService.ShowSuccessAsync(
                SuccessTitle,
                $"A máquina {MaquinaEmEdicao.NumeroDisplay} foi atualizada com sucesso.");

            IsEditMaquinaVisible = false;
            ResetFormularioEdicao();
            await RefreshMaquinasAsync(forceReload: true, resetToFirstPage: false);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSavingEdicao = false;
        }
    }

    [RelayCommand]
    private async Task EliminarMaquinaAsync(MaquinaItem? maquina)
    {
        if (!CanDeleteMachine)
            return;

        if (maquina is null || maquina.Maquina_id <= 0)
            return;

        var confirmar = await _dialogService.ConfirmDeleteAsync($"a máquina {maquina.NumeroDisplay}");
        if (!confirmar)
            return;

        ErrorMessage = string.Empty;

        try
        {
            await _maquinasService.DeleteAsync(maquina.Maquina_id);

            await _dialogService.ShowSuccessAsync(
                SuccessTitle,
                $"A máquina {maquina.NumeroDisplay} foi eliminada com sucesso.");

            await RefreshMaquinasAsync(forceReload: true, resetToFirstPage: false);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task EnsureFasesLoadedAsync(bool forceReload = false)
    {
        if (_fasesLoaded && !forceReload)
            return;

        var primeiraPagina = await _fasesProducaoService.GetAllAsync(1, 100);
        if (primeiraPagina is null)
            throw new InvalidOperationException("Não foi possível carregar as fases de produção.");

        var fases = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _fasesProducaoService.GetAllAsync(page, 100);
            if (pagina?.Items is null)
                continue;

            fases.AddRange(pagina.Items);
        }

        _fasesPorId.Clear();
        FasesDedicadas.Clear();
        FasesProducao.Clear();

        foreach (var fase in fases.OrderBy(item => item.Nome))
        {
            _fasesPorId[fase.FasesProducao_id] = fase.NomeDisplay;
            FasesDedicadas.Add(new FaseDedicadaOption(fase.FasesProducao_id, fase.Nome, fase.NomeDisplay));
            FasesProducao.Add(fase);
        }

        LoadEstadoOptions();
        LoadFaseNomeOptions();
        LoadFormDefaults();
        LoadFormDefaultsFase();
        _fasesLoaded = true;
        NotifyFasesStateChanged();
    }

    private async Task EnsurePermissionsLoadedAsync(bool forceRefresh = false)
    {
        if (_permissionsLoaded && !forceRefresh)
            return;

        await _authorizationService.GetCurrentRoleAsync(forceRefresh);

        CanCreateMachine = _authorizationService.CanCreateMachines();
        CanDeleteMachine = _authorizationService.CanDeleteMachines();
        CanManageProductionPhases = _authorizationService.CanManageProductionPhases();
        CanEditMachine = _authorizationService.CanEditMachineState();
        _permissionsLoaded = true;
    }

    private async Task RefreshMaquinasAsync(bool forceReload, bool resetToFirstPage)
    {
        await ExecutePagedLoadAsync(async () =>
        {
            if (forceReload || !_maquinasLoaded)
            {
                var maquinas = await GetMaquinasAsync(_loadedSearchTerm);
                if (maquinas is null)
                {
                    ErrorMessage = "Não foi possível carregar as máquinas.";
                    _todasMaquinas.Clear();
                    _maquinasFiltradas.Clear();
                    _maquinasLoaded = false;
                    _loadedSearchTerm = string.Empty;
                    Maquinas.Clear();
                    UpdatePagination(0, 1);
                    NotifyCollectionStateChanged();
                    return;
                }

                _todasMaquinas.Clear();
                _todasMaquinas.AddRange(maquinas.OrderBy(item => item.Numero).ThenBy(item => item.NomeModeloDisplay));
                _maquinasLoaded = true;
            }

            if (resetToFirstPage)
                Page = 1;

            ApplyPaginationFromLoadedData();
        });
    }

    private async Task PesquisarAsync()
    {
        ErrorMessage = string.Empty;
        _loadedSearchTerm = SearchTerm.Trim();
        Page = 1;
        await RefreshMaquinasAsync(forceReload: true, resetToFirstPage: true);
    }

    private async Task LimparPesquisaAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchTerm) && string.IsNullOrWhiteSpace(_loadedSearchTerm))
            return;

        ErrorMessage = string.Empty;
        _loadedSearchTerm = string.Empty;
        SearchTerm = string.Empty;
        Page = 1;
        await RefreshMaquinasAsync(forceReload: true, resetToFirstPage: true);
    }

    private async Task<List<MaquinaItem>?> GetMaquinasAsync(string? searchTerm)
    {
        var primeiraPagina = string.IsNullOrWhiteSpace(searchTerm)
            ? await _maquinasService.GetAllAsync(1, 100)
            : await _maquinasService.SearchAsync(searchTerm, 1, 100);

        if (primeiraPagina is null)
            return null;

        var maquinas = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = string.IsNullOrWhiteSpace(searchTerm)
                ? await _maquinasService.GetAllAsync(page, 100)
                : await _maquinasService.SearchAsync(searchTerm, page, 100);

            if (pagina?.Items is null)
                continue;

            maquinas.AddRange(pagina.Items);
        }

        foreach (var maquina in maquinas)
        {
            if (_fasesPorId.TryGetValue(maquina.FaseDedicada_id, out var faseNome))
                maquina.FaseDedicadaNome = faseNome;
        }

        return maquinas;
    }

    private void ApplyPaginationFromLoadedData()
    {
        _maquinasFiltradas.Clear();
        _maquinasFiltradas.AddRange(_todasMaquinas.OrderBy(item => item.Numero).ThenBy(item => item.NomeModeloDisplay));

        var totalFiltrado = _maquinasFiltradas.Count;
        var totalPaginas = totalFiltrado <= 0 ? 1 : (int)Math.Ceiling((double)totalFiltrado / PageSize);
        UpdatePagination(totalFiltrado, totalPaginas);

        var paginaAtual = _maquinasFiltradas
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        Maquinas.Clear();
        foreach (var maquina in paginaAtual)
            Maquinas.Add(maquina);

        NotifyCollectionStateChanged();
    }

    private void LoadEstadoOptions()
    {
        if (EstadoMaquinaOptions.Count > 0)
            return;

        foreach (var option in GetEstadoOptions())
            EstadoMaquinaOptions.Add(option);
    }

    private void LoadFormDefaults()
    {
        SelectedEstadoMaquinaOption ??= EstadoMaquinaOptions[0];
        SelectedFaseDedicadaOption ??= FasesDedicadas[0];
    }

    private void LoadFaseNomeOptions()
    {
        if (FaseNomeOptions.Count > 0)
            return;

        foreach (var option in GetFaseNomeOptions())
            FaseNomeOptions.Add(option);
    }

    private void LoadFormDefaultsFase()
    {
        SelectedFaseNomeOption ??= FaseNomeOptions[0];
    }

    private void ResetFormulario()
    {
        NovoMaquinaId = string.Empty;
        NovoNumero = string.Empty;
        NovoNomeModelo = string.Empty;
        NovoIpAddress = string.Empty;
        SelectedEstadoMaquinaOption = EstadoMaquinaOptions[0];
        SelectedFaseDedicadaOption = FasesDedicadas[0];
        ErrorMessage = string.Empty;
    }

    private void ResetFormularioEdicao()
    {
        MaquinaEmEdicao = null;
        EditarIpAddress = string.Empty;
        SelectedEditEstadoMaquinaOption = null;
        EditEstadoMaquinaOptions.Clear();
        ErrorMessage = string.Empty;
    }

    private void ResetFormularioFase()
    {
        SelectedFaseNomeOption = FaseNomeOptions[0];
        NovaDescricaoFase = string.Empty;
        FasesErrorMessage = string.Empty;
    }

    private void NotifyFasesStateChanged()
    {
        OnPropertyChanged(nameof(HasFasesProducao));
        OnPropertyChanged(nameof(EmptyFasesMessage));
    }

    private void NotifyCollectionStateChanged()
    {
        OnPropertyChanged(nameof(HasMaquinas));
        OnPropertyChanged(nameof(EmptyMessage));
        OnPropertyChanged(nameof(TotalMaquinasDisponiveis));
        OnPropertyChanged(nameof(TotalMaquinasEmUso));
        OnPropertyChanged(nameof(TotalMaquinasManutencao));
        OnPropertyChanged(nameof(TotalMaquinasComConexao));
    }

    private string BuildValidationMessage()
    {
        if (!int.TryParse(NovoMaquinaId.Trim(), out var maquinaId) || maquinaId <= 0)
            return "Indique um identificador interno válido para a máquina.";

        if (!int.TryParse(NovoNumero.Trim(), out var numero) || numero <= 0)
            return "Indique um número físico válido para a máquina.";

        if (string.IsNullOrWhiteSpace(NovoNomeModelo))
            return "Indique o nome ou modelo da máquina.";

        if (SelectedEstadoMaquinaOption is null)
            return "Selecione o estado da máquina.";

        if (SelectedFaseDedicadaOption is null)
            return "Selecione a fase dedicada da máquina.";

        return string.Empty;
    }

    private bool HasMudancasEdicao()
    {
        if (MaquinaEmEdicao is null || SelectedEditEstadoMaquinaOption is null)
            return false;

        var ipAtual = NormalizeOptional(MaquinaEmEdicao.IpAddress);
        var ipNovo = NormalizeOptional(EditarIpAddress);
        var estadoAtual = NormalizeEstado(MaquinaEmEdicao.Estado);
        var estadoNovo = NormalizeEstado(SelectedEditEstadoMaquinaOption.Value);

        return !string.Equals(ipAtual, ipNovo, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(estadoAtual, estadoNovo, StringComparison.OrdinalIgnoreCase);
    }

    private string BuildTransicoesPermitidasDisplay()
    {
        if (MaquinaEmEdicao is null)
            return "Sem transicoes disponiveis.";

        return NormalizeEstado(MaquinaEmEdicao.Estado) switch
        {
            EstadoDisponivel => "Transicoes permitidas: Disponivel -> Manutencao.",
            EstadoEmUso => "Transicoes permitidas: Em Uso -> Manutencao.",
            EstadoManutencao => "Transicoes permitidas: Manutencao -> Disponivel.",
            _ => "Transicoes permitidas: manter estado atual."
        };
    }

    private static IReadOnlyList<EstadoMaquinaOption> GetEstadoOptions()
    {
        return
        [
            new EstadoMaquinaOption(EstadoDisponivel, EstadoDisponivelDisplay),
            new EstadoMaquinaOption(EstadoEmUso, EstadoEmUsoDisplay),
            new EstadoMaquinaOption(EstadoManutencao, EstadoManutencaoDisplay)
        ];
    }

    private static IReadOnlyList<FaseNomeOption> GetFaseNomeOptions()
    {
        return
        [
            new FaseNomeOption("MAQUINACAO", "Maquinacao"),
            new FaseNomeOption("EROSAO", "Erosao"),
            new FaseNomeOption("MONTAGEM", "Montagem")
        ];
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeEstado(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    private void NotifyCanAdicionarMaquinaChanged()
    {
        OnPropertyChanged(nameof(CanAdicionarMaquina));
        ConfirmarAdicionarMaquinaCommand.NotifyCanExecuteChanged();
    }

    private void NotifyCanAdicionarFaseChanged()
    {
        OnPropertyChanged(nameof(CanAdicionarFase));
        ConfirmarAdicionarFaseCommand.NotifyCanExecuteChanged();
    }

    private void NotifyCanGuardarEdicaoChanged()
    {
        OnPropertyChanged(nameof(CanGuardarEdicao));
        GuardarEdicaoMaquinaCommand.NotifyCanExecuteChanged();
    }
}

public sealed record EstadoMaquinaOption(string Value, string DisplayName);

public sealed record FaseDedicadaOption(int Id, string Value, string DisplayName);

public sealed record FaseNomeOption(string Value, string DisplayName);
