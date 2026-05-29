using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class AdicionarEncomendaViewModel : ObservableObject
{
    private const int ClientesPageSize = 5;
    private const int MoldesPageSize = 6;

    private readonly ClientesService _clientesService;
    private readonly MoldesService _moldesService;
    private readonly EncomendasService _encomendasService;
    private readonly IDialogService _dialogService;
    private readonly List<SelectableClienteItem> _todosClientes = [];
    private readonly List<SelectableMoldeItem> _todosMoldes = [];
    private bool _loaded;

    public AdicionarEncomendaViewModel(
        ClientesService clientesService,
        MoldesService moldesService,
        EncomendasService encomendasService,
        IDialogService dialogService)
    {
        _clientesService = clientesService;
        _moldesService = moldesService;
        _encomendasService = encomendasService;
        _dialogService = dialogService;
    }

    public ObservableCollection<SelectableClienteItem> ClientesVisiveis { get; } = new();
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
    private DateTime dataEntregaPrevista = DateTime.Today.AddDays(7);

    [ObservableProperty]
    private string clienteSearchTerm = string.Empty;

    [ObservableProperty]
    private string moldeSearchTerm = string.Empty;

    [ObservableProperty]
    private int clientePage = 1;

    [ObservableProperty]
    private int moldePage = 1;

    [ObservableProperty]
    private bool isLoadingData;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasClientes => ClientesVisiveis.Count > 0;
    public bool HasMoldes => MoldesVisiveis.Count > 0;
    public bool HasSelectedMoldes => MoldesSelecionados.Count > 0;
    public bool HasNoSelectedMoldes => MoldesSelecionados.Count == 0;
    public int ClienteTotalPages => CalculateTotalPages(GetFilteredClientes().Count, ClientesPageSize);
    public int MoldeTotalPages => CalculateTotalPages(GetFilteredMoldes().Count, MoldesPageSize);
    public bool CanGoPreviousClientes => ClientePage > 1;
    public bool CanGoNextClientes => ClientePage < ClienteTotalPages;
    public bool CanGoPreviousMoldes => MoldePage > 1;
    public bool CanGoNextMoldes => MoldePage < MoldeTotalPages;
    public SelectableClienteItem? SelectedClienteItem => _todosClientes.FirstOrDefault(item => item.IsSelected);
    public string SelectedClienteSummary => SelectedClienteItem is null
        ? "Nenhum cliente selecionado."
        : $"Cliente selecionado: {SelectedClienteItem.NomeDisplay}";
    public bool CanCreate => !IsSaving &&
                             SelectedClienteItem is not null &&
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

    partial void OnClienteSearchTermChanged(string value)
    {
        ClientePage = 1;
        RefreshClientes();
    }

    partial void OnMoldeSearchTermChanged(string value)
    {
        MoldePage = 1;
        RefreshMoldes();
    }

    partial void OnClientePageChanged(int value)
    {
        RefreshClientes();
    }

    partial void OnMoldePageChanged(int value)
    {
        RefreshMoldes();
    }

    partial void OnIsSavingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCreate));
        CreateCommand.NotifyCanExecuteChanged();
    }

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

            _todosClientes.Clear();
            foreach (var cliente in clientesTask.Result.OrderBy(cliente => cliente.Nome))
                _todosClientes.Add(new SelectableClienteItem(cliente));

            _todosMoldes.Clear();
            foreach (var molde in moldesTask.Result.OrderBy(molde => molde.Nome).ThenBy(molde => molde.Numero))
            {
                var item = new SelectableMoldeItem(molde);
                item.PropertyChanged += OnSelectableMoldePropertyChanged;
                _todosMoldes.Add(item);
            }

            NomeServicoOptions.Clear();
            foreach (var servico in GetNomeServicoOptions())
                NomeServicoOptions.Add(servico);

            ClientePage = 1;
            MoldePage = 1;
            RefreshClientes();
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
    private async Task Voltar()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private void SelecionarCliente(SelectableClienteItem? cliente)
    {
        if (cliente is null)
            return;

        foreach (var item in _todosClientes)
            item.IsSelected = ReferenceEquals(item, cliente);

        RefreshClientes();
        OnPropertyChanged(nameof(SelectedClienteSummary));
        OnPropertyChanged(nameof(CanCreate));
        CreateCommand.NotifyCanExecuteChanged();

        if (SelectedClienteItem is not null &&
            string.Equals(ErrorMessage, "Selecione exatamente um cliente.", StringComparison.Ordinal))
        {
            ErrorMessage = string.Empty;
        }
    }

    [RelayCommand]
    private void PreviousClientesPage()
    {
        if (CanGoPreviousClientes)
            ClientePage--;
    }

    [RelayCommand]
    private void NextClientesPage()
    {
        if (CanGoNextClientes)
            ClientePage++;
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

    [RelayCommand]
    private void AplicarDataBaseAosMoldesSelecionados()
    {
        foreach (var molde in _todosMoldes.Where(item => item.IsSelected))
            molde.DataEntregaPrevista = DataEntregaPrevista;

        RefreshSelectedMoldes();
    }

    [RelayCommand]
    private void SubirPrioridadeMolde(SelectableMoldeItem? molde)
    {
        if (molde is null || !molde.IsSelected || molde.Prioridade <= 1)
            return;

        var selected = _todosMoldes
            .Where(item => item.IsSelected)
            .OrderBy(item => item.Prioridade)
            .ToList();

        var index = selected.FindIndex(item => ReferenceEquals(item, molde));
        if (index <= 0)
            return;

        var anterior = selected[index - 1];
        (anterior.Prioridade, molde.Prioridade) = (molde.Prioridade, anterior.Prioridade);

        RebalanceSelectedMoldes();
    }

    [RelayCommand]
    private void DescerPrioridadeMolde(SelectableMoldeItem? molde)
    {
        if (molde is null || !molde.IsSelected)
            return;

        var selected = _todosMoldes
            .Where(item => item.IsSelected)
            .OrderBy(item => item.Prioridade)
            .ToList();

        var index = selected.FindIndex(item => ReferenceEquals(item, molde));
        if (index < 0 || index >= selected.Count - 1)
            return;

        var seguinte = selected[index + 1];
        (seguinte.Prioridade, molde.Prioridade) = (molde.Prioridade, seguinte.Prioridade);

        RebalanceSelectedMoldes();
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task Create()
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
            var numeroEncomenda = NumeroEncomendaCliente.Trim();
            var numeroProjeto = NormalizeOptional(NumeroProjetoCliente);
            var nomeResponsavel = NormalizeOptional(NomeResponsavelCliente);

            var encomendaCriada = await _encomendasService.CreateAsync(
                SelectedClienteItem!.Cliente.Cliente_id,
                numeroEncomenda,
                numeroProjeto,
                NomeServicoCliente,
                nomeResponsavel);

            if (encomendaCriada is null || encomendaCriada.Encomenda_id <= 0)
                throw new InvalidOperationException("A encomenda foi enviada, mas nao foi possivel confirmar a criacao.");

            var moldesSelecionados = MoldesSelecionados.ToList();

            try
            {
                for (var index = 0; index < moldesSelecionados.Count; index++)
                {
                    var moldeSelecionado = moldesSelecionados[index];
                    await _encomendasService.CreateEncomendaMoldeAsync(
                        encomendaCriada.Encomenda_id,
                        moldeSelecionado.Molde.MoldeId,
                        quantidade: 1,
                        prioridade: moldeSelecionado.Prioridade,
                        dataEntregaPrevista: moldeSelecionado.DataEntregaPrevista);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"A encomenda {numeroEncomenda} foi criada, mas falhou a associacao de um ou mais moldes. Detalhe: {ex.Message}");
            }

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                $"A encomenda {numeroEncomenda} foi criada com sucesso com {moldesSelecionados.Count} molde(s) associado(s).");

            await Shell.Current.GoToAsync("..");
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

        if (!string.Equals(e.PropertyName, nameof(SelectableMoldeItem.IsSelected), StringComparison.Ordinal))
            return;

        if (molde.IsSelected)
        {
            molde.Prioridade = GetNextPrioridade();
            molde.DataEntregaPrevista = DataEntregaPrevista;
        }
        else
        {
            molde.Prioridade = 0;
        }

        RebalanceSelectedMoldes();

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

    private void RefreshClientes()
    {
        var filtered = GetFilteredClientes();

        if (ClientePage > CalculateTotalPages(filtered.Count, ClientesPageSize))
            ClientePage = CalculateTotalPages(filtered.Count, ClientesPageSize);

        ClientesVisiveis.Clear();
        foreach (var cliente in filtered.Skip((ClientePage - 1) * ClientesPageSize).Take(ClientesPageSize))
            ClientesVisiveis.Add(cliente);

        OnPropertyChanged(nameof(HasClientes));
        OnPropertyChanged(nameof(ClienteTotalPages));
        OnPropertyChanged(nameof(CanGoPreviousClientes));
        OnPropertyChanged(nameof(CanGoNextClientes));
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
            .OrderBy(item => item.Prioridade)
            .ThenBy(item => item.NumeroDisplay)
            .ToList();

        MoldesSelecionados.Clear();

        for (var index = 0; index < selected.Count; index++)
        {
            var item = selected[index];
            item.CanMoveUp = index > 0;
            item.CanMoveDown = index < selected.Count - 1;
            MoldesSelecionados.Add(item);
        }

        OnPropertyChanged(nameof(HasSelectedMoldes));
        OnPropertyChanged(nameof(HasNoSelectedMoldes));
        OnPropertyChanged(nameof(SelectedMoldesCount));
        OnPropertyChanged(nameof(SelectedMoldesSummary));
    }

    private void RebalanceSelectedMoldes()
    {
        var selected = _todosMoldes
            .Where(item => item.IsSelected)
            .OrderBy(item => item.Prioridade <= 0 ? int.MaxValue : item.Prioridade)
            .ThenBy(item => item.NumeroDisplay)
            .ToList();

        for (var index = 0; index < selected.Count; index++)
            selected[index].Prioridade = index + 1;

        RefreshSelectedMoldes();
    }

    private List<SelectableClienteItem> GetFilteredClientes()
    {
        IEnumerable<SelectableClienteItem> query = _todosClientes;

        if (!string.IsNullOrWhiteSpace(ClienteSearchTerm))
        {
            var term = ClienteSearchTerm.Trim();
            query = query.Where(item =>
                item.NomeDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.SiglaDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.NifDisplay.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
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

        if (SelectedClienteItem is null)
            return "Selecione exatamente um cliente.";

        if (string.IsNullOrWhiteSpace(NomeServicoCliente))
            return "Selecione um nome de servico.";

        if (SelectedMoldesCount <= 0)
            return "Selecione pelo menos um molde.";

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

    private int GetNextPrioridade()
    {
        return _todosMoldes
            .Where(item => item.IsSelected)
            .Select(item => item.Prioridade)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }
}

public partial class SelectableClienteItem : ObservableObject
{
    public SelectableClienteItem(ClienteDto cliente)
    {
        Cliente = cliente;
    }

    public ClienteDto Cliente { get; }

    [ObservableProperty]
    private bool isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(BackgroundColor));
    }

    public string NomeDisplay => string.IsNullOrWhiteSpace(Cliente.Nome) ? "Cliente sem nome" : Cliente.Nome;
    public string SiglaDisplay => string.IsNullOrWhiteSpace(Cliente.Sigla) ? "Sem sigla" : Cliente.Sigla;
    public string NifDisplay => string.IsNullOrWhiteSpace(Cliente.NIF) ? "Sem NIF" : Cliente.NIF;
    public string BackgroundColor => IsSelected ? "#EFF6FF" : "White";
}

public partial class SelectableMoldeItem : ObservableObject
{
    public SelectableMoldeItem(MoldeDto molde)
    {
        Molde = molde;
    }

    public MoldeDto Molde { get; }

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private int prioridade;

    [ObservableProperty]
    private DateTime dataEntregaPrevista = DateTime.Today.AddDays(7);

    [ObservableProperty]
    private bool canMoveUp;

    [ObservableProperty]
    private bool canMoveDown;

    public string NomeDisplay => string.IsNullOrWhiteSpace(Molde.Nome) ? "Molde sem nome" : Molde.Nome;
    public string NumeroDisplay => string.IsNullOrWhiteSpace(Molde.Numero) ? "Sem numero" : Molde.Numero;
    public string DescricaoDisplay => string.IsNullOrWhiteSpace(Molde.Descricao) ? "Sem descricao" : Molde.Descricao;
}

public sealed record NomeServicoOption(string Value, string DisplayName);
