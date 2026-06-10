using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

public partial class MaquinasViewModel : PaginatedViewModel
{
    private readonly MaquinasService _maquinasService;
    private readonly FasesProducaoService _fasesProducaoService;
    private readonly IDialogService _dialogService;
    private readonly Dictionary<int, string> _fasesPorId = [];
    private readonly List<MaquinaItem> _todasMaquinas = [];
    private readonly List<MaquinaItem> _maquinasFiltradas = [];
    private bool _fasesLoaded;
    private bool _maquinasLoaded;

    public MaquinasViewModel(
        MaquinasService maquinasService,
        FasesProducaoService fasesProducaoService,
        IDialogService dialogService)
    {
        _maquinasService = maquinasService;
        _fasesProducaoService = fasesProducaoService;
        _dialogService = dialogService;
        PageSize = 8;

        PropertyChanged += (_, e) =>
        {
            if (string.Equals(e.PropertyName, nameof(IsLoading), StringComparison.Ordinal))
            {
                OnPropertyChanged(nameof(CanAdicionarMaquina));
                ConfirmarAdicionarMaquinaCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(CanAdicionarFase));
                ConfirmarAdicionarFaseCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(CanGuardarEdicao));
                GuardarEdicaoMaquinaCommand.NotifyCanExecuteChanged();
            }
        };
    }

    public ObservableCollection<MaquinaItem> Maquinas { get; } = new();
    public ObservableCollection<EstadoMaquinaOption> EstadoMaquinaOptions { get; } = new();
    public ObservableCollection<EstadoMaquinaOption> EditEstadoMaquinaOptions { get; } = new();
    public ObservableCollection<FaseDedicadaOption> FasesDedicadas { get; } = new();
    public ObservableCollection<FaseProducaoItem> FasesProducao { get; } = new();
    public ObservableCollection<FaseNomeOption> FaseNomeOptions { get; } = new();

    [ObservableProperty]
    private bool isAddMaquinaVisible;

    [ObservableProperty]
    private bool isFasesProducaoVisible;

    [ObservableProperty]
    private bool isEditMaquinaVisible;

    [ObservableProperty]
    private string searchTerm = string.Empty;

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

    public bool HasMaquinas => Maquinas.Count > 0;
    public bool HasFasesProducao => FasesProducao.Count > 0;
    public string EmptyMessage => string.IsNullOrWhiteSpace(SearchTerm)
        ? "Nao existem maquinas registadas para apresentar."
        : "Nenhuma maquina corresponde aos filtros atuais.";
    public string EmptyFasesMessage => "Nao existem fases de producao registadas.";
    public int TotalMaquinasDisponiveis => _maquinasFiltradas.Count(item => item.Disponivel);
    public int TotalMaquinasEmUso => _maquinasFiltradas.Count(item => item.EmUtilizacao);
    public int TotalMaquinasManutencao => _maquinasFiltradas.Count(item => item.EmManutencao);
    public int TotalMaquinasComConexao => _maquinasFiltradas.Count(item => item.HasIpAddress);
    public bool CanAdicionarMaquina => !IsLoading
                                       && !IsSaving
                                       && !string.IsNullOrWhiteSpace(NovoMaquinaId)
                                       && !string.IsNullOrWhiteSpace(NovoNumero)
                                       && !string.IsNullOrWhiteSpace(NovoNomeModelo)
                                       && SelectedEstadoMaquinaOption is not null
                                       && SelectedFaseDedicadaOption is not null;
    public bool HasFasesError => !string.IsNullOrWhiteSpace(FasesErrorMessage);
    public bool CanAdicionarFase => !IsLoading
                                    && !IsSavingFase
                                    && SelectedFaseNomeOption is not null;
    public bool CanGuardarEdicao => !IsLoading
                                    && !IsSavingEdicao
                                    && MaquinaEmEdicao is not null
                                    && SelectedEditEstadoMaquinaOption is not null
                                    && HasMudancasEdicao();
    public string MaquinaEmEdicaoDisplay => MaquinaEmEdicao is null
        ? "Sem maquina selecionada."
        : $"{MaquinaEmEdicao.NumeroDisplay} - {MaquinaEmEdicao.NomeModeloDisplay}";
    public string EstadoAtualEdicaoDisplay => MaquinaEmEdicao?.EstadoDisplay ?? "Sem estado";
    public string TransicoesPermitidasDisplay => BuildTransicoesPermitidasDisplay();

    partial void OnSearchTermChanged(string value)
    {
        if (Page != 1)
            Page = 1;

        ApplyFiltersAndPagination();
    }

    partial void OnNovoMaquinaIdChanged(string value)
    {
        OnPropertyChanged(nameof(CanAdicionarMaquina));
        ConfirmarAdicionarMaquinaCommand.NotifyCanExecuteChanged();
    }

    partial void OnNovoNumeroChanged(string value)
    {
        OnPropertyChanged(nameof(CanAdicionarMaquina));
        ConfirmarAdicionarMaquinaCommand.NotifyCanExecuteChanged();
    }

    partial void OnNovoNomeModeloChanged(string value)
    {
        OnPropertyChanged(nameof(CanAdicionarMaquina));
        ConfirmarAdicionarMaquinaCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedEstadoMaquinaOptionChanged(EstadoMaquinaOption? value)
    {
        OnPropertyChanged(nameof(CanAdicionarMaquina));
        ConfirmarAdicionarMaquinaCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedFaseDedicadaOptionChanged(FaseDedicadaOption? value)
    {
        OnPropertyChanged(nameof(CanAdicionarMaquina));
        ConfirmarAdicionarMaquinaCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSavingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanAdicionarMaquina));
        ConfirmarAdicionarMaquinaCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSavingEdicaoChanged(bool value)
    {
        OnPropertyChanged(nameof(CanGuardarEdicao));
        GuardarEdicaoMaquinaCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedFaseNomeOptionChanged(FaseNomeOption? value)
    {
        FasesErrorMessage = string.Empty;
        OnPropertyChanged(nameof(CanAdicionarFase));
        ConfirmarAdicionarFaseCommand.NotifyCanExecuteChanged();
    }

    partial void OnNovaDescricaoFaseChanged(string value)
    {
        FasesErrorMessage = string.Empty;
    }

    partial void OnIsSavingFaseChanged(bool value)
    {
        OnPropertyChanged(nameof(CanAdicionarFase));
        ConfirmarAdicionarFaseCommand.NotifyCanExecuteChanged();
    }

    partial void OnFasesErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasFasesError));
    }

    partial void OnMaquinaEmEdicaoChanged(MaquinaItem? value)
    {
        OnPropertyChanged(nameof(MaquinaEmEdicaoDisplay));
        OnPropertyChanged(nameof(EstadoAtualEdicaoDisplay));
        OnPropertyChanged(nameof(TransicoesPermitidasDisplay));
        OnPropertyChanged(nameof(CanGuardarEdicao));
        GuardarEdicaoMaquinaCommand.NotifyCanExecuteChanged();
    }

    partial void OnEditarIpAddressChanged(string value)
    {
        OnPropertyChanged(nameof(CanGuardarEdicao));
        GuardarEdicaoMaquinaCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedEditEstadoMaquinaOptionChanged(EstadoMaquinaOption? value)
    {
        OnPropertyChanged(nameof(CanGuardarEdicao));
        GuardarEdicaoMaquinaCommand.NotifyCanExecuteChanged();
    }

    public async Task LoadAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            await EnsureFasesLoadedAsync();
            await RefreshMaquinasAsync(forceReload: true, resetToFirstPage: false);
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
        ErrorMessage = string.Empty;
        ApplyFiltersAndPagination();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RecarregarAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            await EnsureFasesLoadedAsync(forceReload: true);
            await RefreshMaquinasAsync(forceReload: true, resetToFirstPage: false);
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
        if (SelectedFaseNomeOption is null)
        {
            FasesErrorMessage = "Selecione o nome da fase de producao.";
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
                "Sucesso",
                $"A fase de producao {SelectedFaseNomeOption.DisplayName} foi criada com sucesso.");

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
        if (fase is null || fase.FasesProducao_id <= 0)
            return;

        var confirmar = await _dialogService.ConfirmDeleteAsync($"a fase de producao {fase.NomeDisplay}");
        if (!confirmar)
            return;

        FasesErrorMessage = string.Empty;

        try
        {
            await _fasesProducaoService.DeleteAsync(fase.FasesProducao_id);

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                $"A fase de producao {fase.NomeDisplay} foi eliminada com sucesso.");

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
                "Sucesso",
                $"A maquina {NovoNumero.Trim()} foi criada com sucesso.");

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
    private async Task EditarMaquinaAsync(MaquinaItem? maquina)
    {
        if (maquina is null)
            return;

        await Shell.Current.GoToAsync(nameof(EditarMaquinaPage), new Dictionary<string, object>
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
                "Sucesso",
                $"A maquina {MaquinaEmEdicao.NumeroDisplay} foi atualizada com sucesso.");

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
        if (maquina is null || maquina.Maquina_id <= 0)
            return;

        var confirmar = await _dialogService.ConfirmDeleteAsync($"a maquina {maquina.NumeroDisplay}");
        if (!confirmar)
            return;

        ErrorMessage = string.Empty;

        try
        {
            await _maquinasService.DeleteAsync(maquina.Maquina_id);

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                $"A maquina {maquina.NumeroDisplay} foi eliminada com sucesso.");

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
            throw new InvalidOperationException("Nao foi possivel carregar as fases de producao.");

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

    private async Task RefreshMaquinasAsync(bool forceReload, bool resetToFirstPage)
    {
        await ExecutePagedLoadAsync(async () =>
        {
            if (forceReload || !_maquinasLoaded)
            {
                var maquinas = await GetAllMaquinasAsync();
                if (maquinas is null)
                {
                    ErrorMessage = "Nao foi possivel carregar as maquinas.";
                    _todasMaquinas.Clear();
                    _maquinasFiltradas.Clear();
                    _maquinasLoaded = false;
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

            ApplyFiltersAndPagination();
        });
    }

    private async Task<List<MaquinaItem>?> GetAllMaquinasAsync()
    {
        var primeiraPagina = await _maquinasService.GetAllAsync(1, 100);
        if (primeiraPagina is null)
            return null;

        var maquinas = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _maquinasService.GetAllAsync(page, 100);
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

    private void ApplyFiltersAndPagination()
    {
        IEnumerable<MaquinaItem> query = _todasMaquinas;

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim();
            query = query.Where(item =>
                item.NomeModeloDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.NumeroDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.EstadoDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.FaseDedicadaDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.IpAddressDisplay.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        _maquinasFiltradas.Clear();
        _maquinasFiltradas.AddRange(query.OrderBy(item => item.Numero).ThenBy(item => item.NomeModeloDisplay));

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
        SelectedEstadoMaquinaOption ??= EstadoMaquinaOptions.FirstOrDefault();
        SelectedFaseDedicadaOption ??= FasesDedicadas.FirstOrDefault();
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
        SelectedFaseNomeOption ??= FaseNomeOptions.FirstOrDefault();
    }

    private void ResetFormulario()
    {
        NovoMaquinaId = string.Empty;
        NovoNumero = string.Empty;
        NovoNomeModelo = string.Empty;
        NovoIpAddress = string.Empty;
        SelectedEstadoMaquinaOption = EstadoMaquinaOptions.FirstOrDefault();
        SelectedFaseDedicadaOption = FasesDedicadas.FirstOrDefault();
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
        SelectedFaseNomeOption = FaseNomeOptions.FirstOrDefault();
        NovaDescricaoFase = string.Empty;
        FasesErrorMessage = string.Empty;
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

    private void NotifyFasesStateChanged()
    {
        OnPropertyChanged(nameof(HasFasesProducao));
        OnPropertyChanged(nameof(EmptyFasesMessage));
    }

    private string BuildValidationMessage()
    {
        if (!int.TryParse(NovoMaquinaId.Trim(), out var maquinaId) || maquinaId <= 0)
            return "Indique um identificador interno valido para a maquina.";

        if (!int.TryParse(NovoNumero.Trim(), out var numero) || numero <= 0)
            return "Indique um numero fisico valido para a maquina.";

        if (string.IsNullOrWhiteSpace(NovoNomeModelo))
            return "Indique o nome ou modelo da maquina.";

        if (SelectedEstadoMaquinaOption is null)
            return "Selecione o estado da maquina.";

        if (SelectedFaseDedicadaOption is null)
            return "Selecione a fase dedicada da maquina.";

        return string.Empty;
    }

    private void LoadEstadoOptionsParaEdicao(MaquinaItem maquina)
    {
        EditEstadoMaquinaOptions.Clear();

        foreach (var option in GetEstadoOptionsParaEdicao(maquina.Estado))
            EditEstadoMaquinaOptions.Add(option);
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
            "DISPONIVEL" => "Transicoes permitidas: Disponivel -> Manutencao.",
            "EM_USO" => "Transicoes permitidas: Em Uso -> Manutencao.",
            "MANUTENCAO" => "Transicoes permitidas: Manutencao -> Disponivel.",
            _ => "Transicoes permitidas: manter estado atual."
        };
    }

    private static IReadOnlyList<EstadoMaquinaOption> GetEstadoOptions()
    {
        return
        [
            new EstadoMaquinaOption("DISPONIVEL", "Disponivel"),
            new EstadoMaquinaOption("EM_USO", "Em Uso"),
            new EstadoMaquinaOption("MANUTENCAO", "Manutencao")
        ];
    }

    private static IReadOnlyList<EstadoMaquinaOption> GetEstadoOptionsParaEdicao(string? estadoAtual)
    {
        return NormalizeEstado(estadoAtual) switch
        {
            "DISPONIVEL" =>
            [
                new EstadoMaquinaOption("DISPONIVEL", "Disponivel"),
                new EstadoMaquinaOption("MANUTENCAO", "Manutencao")
            ],
            "EM_USO" =>
            [
                new EstadoMaquinaOption("EM_USO", "Em Uso"),
                new EstadoMaquinaOption("MANUTENCAO", "Manutencao")
            ],
            "MANUTENCAO" =>
            [
                new EstadoMaquinaOption("MANUTENCAO", "Manutencao"),
                new EstadoMaquinaOption("DISPONIVEL", "Disponivel")
            ],
            _ =>
            [
                new EstadoMaquinaOption(NormalizeEstado(estadoAtual), string.IsNullOrWhiteSpace(estadoAtual) ? "Sem estado" : estadoAtual.Replace('_', ' '))
            ]
        };
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
}

public sealed record EstadoMaquinaOption(string Value, string DisplayName);

public sealed record FaseDedicadaOption(int Id, string Value, string DisplayName);

public sealed record FaseNomeOption(string Value, string DisplayName);
