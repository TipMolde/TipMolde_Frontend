using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere a criacao de encomendas e a associacao inicial dos moldes com prioridade global
/// calculada a partir da data de entrega de cada molde.
/// </summary>
public partial class AdicionarEncomendaViewModel : ObservableObject
{
    private const int MoldesPageSize = 6;

    private readonly ClientesService _clientesService;
    private readonly MoldesService _moldesService;
    private readonly EncomendasService _encomendasService;
    private readonly GlobalMoldePriorityService _globalMoldePriorityService;
    private readonly IDialogService _dialogService;
    private readonly List<ClienteOption> _todosClientes = [];
    private readonly List<SelectableMoldeItem> _todosMoldes = [];
    private readonly SemaphoreSlim _priorityRecalculationLock = new(1, 1);
    private bool _loaded;

    /// <summary>
    /// Construtor do view model de criacao de encomendas.
    /// </summary>
    public AdicionarEncomendaViewModel(
        ClientesService clientesService,
        MoldesService moldesService,
        EncomendasService encomendasService,
        GlobalMoldePriorityService globalMoldePriorityService,
        IDialogService dialogService)
    {
        _clientesService = clientesService;
        _moldesService = moldesService;
        _encomendasService = encomendasService;
        _globalMoldePriorityService = globalMoldePriorityService;
        _dialogService = dialogService;
    }

    public ObservableCollection<ClienteOption> ClientesDisponiveis { get; } = new();
    public ObservableCollection<SelectableMoldeItem> MoldesVisiveis { get; } = new();
    public ObservableCollection<SelectableMoldeItem> MoldesSelecionados { get; } = new();
    public ObservableCollection<NomeServicoOption> NomeServicoOptions { get; } = new();

    [ObservableProperty]
    private string numeroEncomendaCliente = string.Empty;

    [ObservableProperty]
    private string numeroProjetoCliente = string.Empty;

    [ObservableProperty]
    private string nomeServicoCliente = string.Empty;

    [ObservableProperty]
    private NomeServicoOption? selectedNomeServicoOption;

    [ObservableProperty]
    private string nomeResponsavelCliente = string.Empty;

    [ObservableProperty]
    private ClienteOption? selectedClienteOption;

    [ObservableProperty]
    private string moldeSearchTerm = string.Empty;

    [ObservableProperty]
    private int moldePage = 1;

    [ObservableProperty]
    private bool isLoadingData;

    [ObservableProperty]
    private bool isCalculatingPriorities;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasClientes => ClientesDisponiveis.Count > 0;
    public bool HasMoldes => MoldesVisiveis.Count > 0;
    public bool HasSelectedMoldes => MoldesSelecionados.Count > 0;
    public bool HasNoSelectedMoldes => MoldesSelecionados.Count == 0;
    public int MoldeTotalPages => CalculateTotalPages(GetFilteredMoldes().Count, MoldesPageSize);
    public bool CanGoPreviousMoldes => MoldePage > 1;
    public bool CanGoNextMoldes => MoldePage < MoldeTotalPages;
    public string SelectedClienteSummary => SelectedClienteOption is null
        ? "Nenhum cliente selecionado."
        : $"Cliente selecionado: {SelectedClienteOption.DisplayName}";
    public bool CanCreate => !IsSaving &&
                             !IsLoadingData &&
                             SelectedClienteOption is not null &&
                             SelectedNomeServicoOption is not null &&
                             SelectedMoldesCount > 0;
    public int SelectedMoldesCount => _todosMoldes.Count(item => item.IsSelected);
    public string SelectedMoldesSummary => SelectedMoldesCount switch
    {
        0 => "Nenhum molde selecionado.",
        1 => "1 molde selecionado.",
        _ => $"{SelectedMoldesCount} moldes selecionados."
    };

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnSelectedNomeServicoOptionChanged(NomeServicoOption? value)
    {
        NomeServicoCliente = value?.Value ?? string.Empty;
        OnPropertyChanged(nameof(CanCreate));
        CreateCommand.NotifyCanExecuteChanged();

        if (value is not null &&
            string.Equals(ErrorMessage, "Selecione um nome de servico.", StringComparison.Ordinal))
        {
            ErrorMessage = string.Empty;
        }
    }

    partial void OnNumeroEncomendaClienteChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            string.Equals(ErrorMessage, "Indique o numero da encomenda.", StringComparison.Ordinal))
        {
            ErrorMessage = string.Empty;
        }
    }

    partial void OnMoldeSearchTermChanged(string value)
    {
        MoldePage = 1;
        RefreshMoldes();
    }

    partial void OnSelectedClienteOptionChanged(ClienteOption? value)
    {
        OnPropertyChanged(nameof(SelectedClienteSummary));
        OnPropertyChanged(nameof(CanCreate));
        CreateCommand.NotifyCanExecuteChanged();

        if (value is not null &&
            string.Equals(ErrorMessage, "Selecione um cliente.", StringComparison.Ordinal))
        {
            ErrorMessage = string.Empty;
        }
    }

    partial void OnMoldePageChanged(int value)
    {
        RefreshMoldes();
    }

    partial void OnIsSavingChanged(bool value)
    {
        UpdateCanCreateState();
    }

    partial void OnIsLoadingDataChanged(bool value)
    {
        if (value == IsLoadingData)
            UpdateCanCreateState();
    }

    /// <summary>
    /// Carrega clientes e moldes disponiveis para compor a nova encomenda.
    /// </summary>
    public async Task LoadAsync()
    {
        if (_loaded || IsLoadingData)
            return;

        IsLoadingData = true;
        ErrorMessage = string.Empty;

        try
        {
            var clientesTask = GetAllClientesAsync();
            var moldesTask = GetAllMoldesAsync();
            await Task.WhenAll(clientesTask, moldesTask);
            var clientes = await clientesTask;
            var moldes = await moldesTask;

            _todosClientes.Clear();
            foreach (var cliente in clientes.OrderBy(cliente => cliente.Nome))
                _todosClientes.Add(new ClienteOption(cliente));

            ClientesDisponiveis.Clear();
            foreach (var cliente in _todosClientes)
                ClientesDisponiveis.Add(cliente);

            SelectedClienteOption ??= ClientesDisponiveis.FirstOrDefault();

            _todosMoldes.Clear();
            foreach (var molde in moldes.OrderBy(molde => molde.Nome).ThenBy(molde => molde.Numero))
            {
                var item = new SelectableMoldeItem(molde);
                item.PropertyChanged += OnSelectableMoldePropertyChanged;
                _todosMoldes.Add(item);
            }

            NomeServicoOptions.Clear();
            foreach (var servico in GetNomeServicoOptions())
                NomeServicoOptions.Add(servico);

            MoldePage = 1;
            RefreshMoldes();
            RefreshSelectedMoldes();

            OnPropertyChanged(nameof(SelectedClienteSummary));
            OnPropertyChanged(nameof(SelectedMoldesCount));
            OnPropertyChanged(nameof(SelectedMoldesSummary));
            OnPropertyChanged(nameof(HasSelectedMoldes));
            OnPropertyChanged(nameof(HasNoSelectedMoldes));
            OnPropertyChanged(nameof(CanCreate));
            CreateCommand.NotifyCanExecuteChanged();

            _loaded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoadingData = false;
        }
    }

    [RelayCommand]
    private static async Task Voltar()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private void PreviousMoldesPage()
    {
        if (CanGoPreviousMoldes)
            MoldePage--;
    }

    [RelayCommand]
    private void NextMoldesPage()
    {
        if (CanGoNextMoldes)
            MoldePage++;
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task Create()
    {
        // Fecha a ordenacao global antes da criacao para os moldes entrarem com prioridades coerentes.
        await RecalculateSelectedMoldesPrioritiesAsync();

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
            await CreateEncomendaWithSelectedMoldesAsync();
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

    private async Task CreateEncomendaWithSelectedMoldesAsync()
    {
        var numeroEncomenda = NumeroEncomendaCliente.Trim();
        var encomendaCriada = await CreateEncomendaAsync(numeroEncomenda);
        var moldesSelecionados = MoldesSelecionados.ToList();

        await AssociateSelectedMoldesAsync(encomendaCriada.Encomenda_id, numeroEncomenda, moldesSelecionados);

        await _dialogService.ShowSuccessAsync(
            "Sucesso",
            $"A encomenda {numeroEncomenda} foi criada com sucesso com {moldesSelecionados.Count} molde(s) associado(s).");

        await Shell.Current.GoToAsync("..");
    }

    private async Task<EncomendaResumoDto> CreateEncomendaAsync(string numeroEncomenda)
    {
        var encomendaCriada = await _encomendasService.CreateAsync(
            SelectedClienteOption!.Cliente.Cliente_id,
            numeroEncomenda,
            NormalizeOptional(NumeroProjetoCliente),
            NomeServicoCliente,
            NormalizeOptional(NomeResponsavelCliente));

        if (encomendaCriada is null || encomendaCriada.Encomenda_id <= 0)
            throw new InvalidOperationException("A encomenda foi enviada, mas nao foi possivel confirmar a criacao.");

        return encomendaCriada;
    }

    private async Task AssociateSelectedMoldesAsync(
        int encomendaId,
        string numeroEncomenda,
        IReadOnlyList<SelectableMoldeItem> moldesSelecionados)
    {
        try
        {
            foreach (var moldeSelecionado in moldesSelecionados.OrderBy(item => item.Prioridade))
            {
                await _encomendasService.CreateEncomendaMoldeAsync(
                    encomendaId,
                    moldeSelecionado.Molde.MoldeId,
                    quantidade: moldeSelecionado.QuantidadePedida,
                    prioridade: moldeSelecionado.Prioridade,
                    dataEntregaPrevista: moldeSelecionado.DataEntregaPrevista);
            }

            await _globalMoldePriorityService.RebalanceAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"A encomenda {numeroEncomenda} foi criada, mas falhou a associacao dos moldes ou o recalculo das prioridades globais. Detalhe: {ex.Message}");
        }
    }

    private async Task<List<ClienteDto>> GetAllClientesAsync()
    {
        var primeiraPagina = await _clientesService.GetClientesAsync(1, 100);
        if (primeiraPagina is null)
            return [];

        var clientes = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _clientesService.GetClientesAsync(page, 100);
            if (pagina?.Items is null)
                continue;

            clientes.AddRange(pagina.Items);
        }

        return clientes;
    }

    private async Task<List<MoldeDto>> GetAllMoldesAsync()
    {
        var primeiraPagina = await _moldesService.GetAllAsync(1, 100);
        if (primeiraPagina is null)
            return [];

        var moldes = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _moldesService.GetAllAsync(page, 100);
            if (pagina?.Items is null)
                continue;

            moldes.AddRange(pagina.Items);
        }

        return moldes;
    }

    private void OnSelectableMoldePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not SelectableMoldeItem molde)
            return;

        if (string.Equals(e.PropertyName, nameof(SelectableMoldeItem.IsSelected), StringComparison.Ordinal))
        {
            if (molde.IsSelected)
            {
                if (molde.DataEntregaPrevista <= DateTime.MinValue)
                    molde.DataEntregaPrevista = DateTime.Today.AddDays(7);

                if (molde.QuantidadePedida <= 0)
                    molde.QuantidadePedida = 1;
            }
            else
            {
                molde.Prioridade = 0;
            }

            OnPropertyChanged(nameof(SelectedMoldesCount));
            OnPropertyChanged(nameof(SelectedMoldesSummary));
            OnPropertyChanged(nameof(CanCreate));
            CreateCommand.NotifyCanExecuteChanged();

            if (SelectedMoldesCount > 0 &&
                string.Equals(ErrorMessage, "Selecione pelo menos um molde.", StringComparison.Ordinal))
            {
                ErrorMessage = string.Empty;
            }
        }

        if (!string.Equals(e.PropertyName, nameof(SelectableMoldeItem.IsSelected), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(SelectableMoldeItem.DataEntregaPrevista), StringComparison.Ordinal))
        {
            return;
        }

        QueuePriorityRecalculation();
    }

    private void QueuePriorityRecalculation()
    {
        _ = RecalculateSelectedMoldesPrioritiesAsync();
    }

    // Porque: a data de entrega de um molde pode alterar a sua posicao face aos moldes de outras
    // encomendas abertas, por isso a prioridade e recalculada sempre a partir do conjunto global.
    private async Task RecalculateSelectedMoldesPrioritiesAsync()
    {
        await _priorityRecalculationLock.WaitAsync();

        try
        {
            IsCalculatingPriorities = true;

            var selected = _todosMoldes
                .Where(item => item.IsSelected)
                .ToList();

            if (selected.Count == 0)
            {
                RefreshSelectedMoldes();
                return;
            }

            var drafts = selected
                .Select(item => new GlobalMoldeDraftPriorityItem(
                    item.DraftKey,
                    EncomendaId: 0,
                    item.Molde.MoldeId,
                    item.DataEntregaPrevista,
                    item.NumeroDisplay))
                .ToList();

            var prioridades = await _globalMoldePriorityService.CalculateDraftPrioritiesAsync(drafts);

            foreach (var item in selected)
            {
                item.Prioridade = prioridades.TryGetValue(item.DraftKey, out var prioridade)
                    ? prioridade
                    : Math.Max(1, item.Prioridade);
            }

            if (ErrorMessage.StartsWith("Nao foi possivel calcular as prioridades globais dos moldes.", StringComparison.Ordinal))
                ErrorMessage = string.Empty;

            RefreshSelectedMoldes();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nao foi possivel calcular as prioridades globais dos moldes. {ex.Message}";
            RefreshSelectedMoldes();
        }
        finally
        {
            IsCalculatingPriorities = false;
            _priorityRecalculationLock.Release();
        }
    }

    private void RefreshMoldes()
    {
        var filtered = GetFilteredMoldes();

        if (MoldePage > CalculateTotalPages(filtered.Count, MoldesPageSize))
            MoldePage = CalculateTotalPages(filtered.Count, MoldesPageSize);

        MoldesVisiveis.Clear();
        foreach (var molde in filtered.Skip((MoldePage - 1) * MoldesPageSize).Take(MoldesPageSize))
            MoldesVisiveis.Add(molde);

        OnPropertyChanged(nameof(HasMoldes));
        OnPropertyChanged(nameof(MoldeTotalPages));
        OnPropertyChanged(nameof(CanGoPreviousMoldes));
        OnPropertyChanged(nameof(CanGoNextMoldes));
    }

    private void RefreshSelectedMoldes()
    {
        var selected = _todosMoldes
            .Where(item => item.IsSelected)
            .OrderBy(item => item.Prioridade <= 0 ? int.MaxValue : item.Prioridade)
            .ThenBy(item => item.DataEntregaPrevista.Date)
            .ThenBy(item => item.NumeroDisplay)
            .ToList();

        MoldesSelecionados.Clear();

        foreach (var item in selected)
            MoldesSelecionados.Add(item);

        OnPropertyChanged(nameof(HasSelectedMoldes));
        OnPropertyChanged(nameof(HasNoSelectedMoldes));
        OnPropertyChanged(nameof(SelectedMoldesCount));
        OnPropertyChanged(nameof(SelectedMoldesSummary));
    }

    private List<SelectableMoldeItem> GetFilteredMoldes()
    {
        IEnumerable<SelectableMoldeItem> query = _todosMoldes;

        if (!string.IsNullOrWhiteSpace(MoldeSearchTerm))
        {
            var term = MoldeSearchTerm.Trim();
            query = query.Where(item =>
                item.NomeDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.NumeroDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.DescricaoDisplay.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
    }

    private static IReadOnlyList<NomeServicoOption> GetNomeServicoOptions()
    {
        return
        [
            new NomeServicoOption("NOVO_MOLDE", "Novo Molde"),
            new NomeServicoOption("REPARACAO", "Reparacao"),
            new NomeServicoOption("ALTERACAO", "Alteracao")
        ];
    }

    private string BuildValidationMessage()
    {
        if (string.IsNullOrWhiteSpace(NumeroEncomendaCliente))
            return "Indique o numero da encomenda.";

        if (SelectedClienteOption is null)
            return "Selecione um cliente.";

        if (string.IsNullOrWhiteSpace(NomeServicoCliente))
            return "Selecione um nome de servico.";

        if (SelectedMoldesCount <= 0)
            return "Selecione pelo menos um molde.";

        if (_todosMoldes.Any(item => item.IsSelected && item.QuantidadePedida <= 0))
            return "Indique uma quantidade valida para todos os moldes selecionados.";

        if (_todosMoldes.Any(item => item.IsSelected && item.DataEntregaPrevista <= DateTime.MinValue))
            return "Indique uma data de entrega valida para todos os moldes selecionados.";

        if (_todosMoldes.Any(item => item.IsSelected && item.Prioridade <= 0))
            return "Aguarde pelo calculo das prioridades globais dos moldes.";

        return string.Empty;
    }

    private static int CalculateTotalPages(int totalItems, int pageSize)
    {
        if (pageSize <= 0)
            return 1;

        return Math.Max(1, (int)Math.Ceiling((double)totalItems / pageSize));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void UpdateCanCreateState()
    {
        OnPropertyChanged(nameof(CanCreate));
        CreateCommand.NotifyCanExecuteChanged();
    }
}

/// <summary>
/// Opcao de cliente apresentada no picker da encomenda.
/// </summary>
public sealed record ClienteOption(ClienteDto Cliente)
{
    public string NomeDisplay => string.IsNullOrWhiteSpace(Cliente.Nome) ? "Cliente sem nome" : Cliente.Nome;
    public string SiglaDisplay => string.IsNullOrWhiteSpace(Cliente.Sigla) ? "Sem sigla" : Cliente.Sigla;
    public string NifDisplay => string.IsNullOrWhiteSpace(Cliente.NIF) ? "Sem NIF" : Cliente.NIF;
    public string DisplayName => $"{NomeDisplay} - {SiglaDisplay}";
}

/// <summary>
/// Item de molde selecionavel no formulario de encomendas, com quantidade, data de entrega e prioridade calculada.
/// </summary>
public partial class SelectableMoldeItem : ObservableObject
{
    public SelectableMoldeItem(MoldeDto molde)
    {
        Molde = molde;
    }

    public MoldeDto Molde { get; }
    public string DraftKey { get; } = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private int prioridade;

    [ObservableProperty]
    private int quantidadePedida = 1;

    [ObservableProperty]
    private DateTime dataEntregaPrevista = DateTime.Today.AddDays(7);

    public string NomeDisplay => string.IsNullOrWhiteSpace(Molde.Nome) ? "Molde sem nome" : Molde.Nome;
    public string NumeroDisplay => string.IsNullOrWhiteSpace(Molde.Numero) ? "Sem numero" : Molde.Numero;
    public string DescricaoDisplay => string.IsNullOrWhiteSpace(Molde.Descricao) ? "Sem descricao" : Molde.Descricao;
    public string ImagemCapaSource => Molde.ImagemCapaSource;

    partial void OnQuantidadePedidaChanged(int value)
    {
        if (value <= 0)
            QuantidadePedida = 1;
    }
}

/// <summary>
/// Opcao de nome de servico apresentada no picker da encomenda.
/// </summary>
public sealed record NomeServicoOption(string Value, string DisplayName);
