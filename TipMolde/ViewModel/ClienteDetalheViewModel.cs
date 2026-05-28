using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

public partial class ClienteDetalheViewModel : PaginatedViewModel
{
    private const int EstadoTodasIndex = 0;
    private const int EstadoEmProducaoIndex = 1;
    private const int EstadoEntreguesIndex = 2;
    private const string ValorNaoDefinido = "Nao definido";

    private readonly ClientesService _clientesService;
    private readonly List<EncomendaResumoDto> _todasEncomendas = [];
    private string _appliedSearchTerm = string.Empty;

    public ClienteDetalheViewModel(ClientesService clientesService)
    {
        _clientesService = clientesService;
        Encomendas.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasEncomendas));
            OnPropertyChanged(nameof(HasNoEncomendas));
        };
        PageSize = 5;
    }

    public ObservableCollection<EncomendaResumoDto> Encomendas { get; } = new();
    public IReadOnlyList<string> EstadoFilters { get; } = new[]
    {
        "Todas",
        "Em producao",
        "Entregues"
    };

    [ObservableProperty]
    private int cliente_id;

    [ObservableProperty]
    private string nome = string.Empty;

    [ObservableProperty]
    private string sigla = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string telefone = string.Empty;

    [ObservableProperty]
    private string pais = string.Empty;

    [ObservableProperty]
    private string nif = string.Empty;

    [ObservableProperty]
    private string searchTerm = string.Empty;

    [ObservableProperty]
    private int selectedEstadoFilterIndex = -1;

    public bool HasEncomendas => Encomendas.Count > 0;
    public bool HasNoEncomendas => Encomendas.Count == 0;
    public bool HasSearch => !string.IsNullOrWhiteSpace(SearchTerm);
    public bool HasPagination => TotalPages > 1;
    public string SiglaDisplay => string.IsNullOrWhiteSpace(Sigla) ? ValorNaoDefinido : Sigla;
    public string NifDisplay => string.IsNullOrWhiteSpace(Nif) ? ValorNaoDefinido : Nif;
    public string PaisDisplay => string.IsNullOrWhiteSpace(Pais) ? ValorNaoDefinido : Pais;
    public string EmailDisplay => string.IsNullOrWhiteSpace(Email) ? ValorNaoDefinido : Email;
    public string TelefoneDisplay => string.IsNullOrWhiteSpace(Telefone) ? ValorNaoDefinido : Telefone;
    public string EmptyEncomendasMessage => _todasEncomendas.Count == 0
        ? "Este cliente ainda nao tem encomendas associadas."
        : "Nenhuma encomenda corresponde aos filtros atuais.";

    partial void OnSearchTermChanged(string value)
    {
        OnPropertyChanged(nameof(HasSearch));
    }

    partial void OnSelectedEstadoFilterIndexChanged(int value)
    {
        _ = ResetToFirstPageAndReloadAsync();
    }

    partial void OnPaisChanged(string value)
    {
        OnPropertyChanged(nameof(PaisDisplay));
    }

    partial void OnSiglaChanged(string value)
    {
        OnPropertyChanged(nameof(SiglaDisplay));
    }

    partial void OnNifChanged(string value)
    {
        OnPropertyChanged(nameof(NifDisplay));
    }

    partial void OnEmailChanged(string value)
    {
        OnPropertyChanged(nameof(EmailDisplay));
    }

    partial void OnTelefoneChanged(string value)
    {
        OnPropertyChanged(nameof(TelefoneDisplay));
    }

    public void EnsureDefaultEstadoFilter()
    {
        if (SelectedEstadoFilterIndex >= 0)
            return;

        SelectedEstadoFilterIndex = EstadoTodasIndex;
    }

    public async Task LoadAsync(int id)
    {
        Cliente_id = id;
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            var result = await _clientesService.GetClienteWithEncomendasAsync(id);

            if (result is null)
            {
                ErrorMessage = "Nao foi possivel carregar o detalhe do cliente.";
                return;
            }

            Nome = result.Nome ?? string.Empty;
            Sigla = result.Sigla ?? string.Empty;
            Email = result.Email ?? string.Empty;
            Telefone = result.Telefone ?? string.Empty;
            Pais = result.Pais ?? string.Empty;
            Nif = result.NIF ?? string.Empty;

            _todasEncomendas.Clear();

            if (result.Encomendas is not null)
                _todasEncomendas.AddRange(result.Encomendas);

            EnsureDefaultEstadoFilter();
            _appliedSearchTerm = SearchTerm.Trim();
            Page = 1;
            await LoadPageAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PesquisarAsync()
    {
        _appliedSearchTerm = SearchTerm.Trim();
        await ResetToFirstPageAndReloadAsync();
    }

    [RelayCommand]
    private async Task LimparPesquisaAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
            return;

        SearchTerm = string.Empty;
        _appliedSearchTerm = string.Empty;
        await ResetToFirstPageAndReloadAsync();
    }

    [RelayCommand]
    private async Task VoltarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    protected override async Task LoadPageAsync()
    {
        var filtered = BuildFilteredEncomendas()
            .OrderByDescending(e => IsEstado(e.Estado, "EM_PRODUCAO"))
            .ThenByDescending(e => e.DataRegisto)
            .ToList();

        var totalPages = filtered.Count == 0
            ? 1
            : (int)Math.Ceiling((double)filtered.Count / PageSize);

        UpdatePagination(filtered.Count, totalPages);

        var items = filtered
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        Encomendas.Clear();

        foreach (var encomenda in items)
            Encomendas.Add(encomenda);

        OnPropertyChanged(nameof(HasPagination));
        OnPropertyChanged(nameof(EmptyEncomendasMessage));

        await Task.CompletedTask;
    }

    private IEnumerable<EncomendaResumoDto> BuildFilteredEncomendas()
    {
        IEnumerable<EncomendaResumoDto> query = _todasEncomendas;

        if (!string.IsNullOrWhiteSpace(_appliedSearchTerm))
        {
            var search = _appliedSearchTerm;
            query = query.Where(e =>
                Contains(e.NomeServicoCliente, search) ||
                Contains(e.NomeResponsavelCliente, search) ||
                Contains(e.NumeroEncomendaCliente, search) ||
                Contains(e.NumeroProjetoCliente, search));
        }

        return SelectedEstadoFilterIndex switch
        {
            EstadoEmProducaoIndex => query.Where(e => IsEstado(e.Estado, "EM_PRODUCAO")),
            EstadoEntreguesIndex => query.Where(e =>
                IsEstado(e.Estado, "CONCLUIDA") ||
                IsEstado(e.Estado, "PARCIALMENTE_ENTREGUE")),
            _ => query
        };
    }

    private static bool Contains(string? source, string value)
    {
        return source?.Contains(value, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsEstado(string? estado, string expected)
    {
        return string.Equals(estado?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }
}
