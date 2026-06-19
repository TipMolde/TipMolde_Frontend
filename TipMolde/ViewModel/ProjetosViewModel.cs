using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

public partial class ProjetosViewModel : PaginatedViewModel
{
    private readonly ProjetosService _projetosService;
    private readonly AuthorizationService _authorizationService;
    private readonly List<ProjetoDto> _todosProjetos = [];
    private readonly List<ProjetoDto> _projetosFiltrados = [];
    private bool _permissionsLoaded;

    public ProjetosViewModel(
        ProjetosService projetosService,
        AuthorizationService authorizationService)
    {
        _projetosService = projetosService;
        _authorizationService = authorizationService;
        PageSize = 8;
    }

    public ObservableCollection<ProjetoDto> Projetos { get; } = new();

    [ObservableProperty]
    private string searchTerm = string.Empty;

    [ObservableProperty]
    private bool isAdmin;

    public bool HasProjetos => Projetos.Count > 0;
    public int TotalProjetos => _projetosFiltrados.Count;
    public int TotalMoldesAssociados => _projetosFiltrados.Select(item => item.Molde_id).Distinct().Count();
    public string EmptyMessage => string.IsNullOrWhiteSpace(SearchTerm)
        ? "Ainda nao existem projetos de desenho registados."
        : "Nenhum projeto corresponde aos filtros atuais.";

    partial void OnSearchTermChanged(string value)
    {
        if (Page != 1)
            Page = 1;

        RefreshProjetos();
        OnPropertyChanged(nameof(EmptyMessage));
    }

    public async Task LoadAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            await ExecutePagedLoadAsync(async () =>
            {
                _todosProjetos.Clear();

                var projetos = await GetAllProjetosAsync();
                _todosProjetos.AddRange(projetos);

                Page = 1;
                RefreshProjetos();
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Projetos.Clear();
            OnPropertyChanged(nameof(HasProjetos));
            OnPropertyChanged(nameof(EmptyMessage));
        }
    }

    protected override Task LoadPageAsync()
    {
        RefreshProjetos();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RecarregarAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AdicionarProjetoAsync()
    {
        if (!IsAdmin)
            return;

        await Shell.Current.GoToAsync(nameof(AdicionarProjetoPage));
    }

    [RelayCommand]
    private async Task AbrirProjetoAsync(ProjetoDto? projeto)
    {
        if (projeto is null || projeto.Projeto_id <= 0)
            return;

        await Shell.Current.GoToAsync($"{nameof(ProjetoDetalhePage)}?projeto_id={projeto.Projeto_id}");
    }

    private async Task<List<ProjetoDto>> GetAllProjetosAsync()
    {
        await EnsurePermissionsLoadedAsync();

        var primeiraPagina = await _projetosService.GetAllAsync(1, 10);
        if (primeiraPagina is null)
            return [];

        var projetos = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _projetosService.GetAllAsync(page, 100);
            if (pagina?.Items is null)
                continue;

            projetos.AddRange(pagina.Items);
        }

        return projetos;
    }

    private async Task EnsurePermissionsLoadedAsync()
    {
        if (_permissionsLoaded)
            return;

        await _authorizationService.GetCurrentRoleAsync();
        IsAdmin = _authorizationService.CanCreateMachines();
        _permissionsLoaded = true;
    }

    private void RefreshProjetos()
    {
        IEnumerable<ProjetoDto> query = _todosProjetos;

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim();
            query = query.Where(item =>
                item.NomeProjetoDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.SoftwareUtilizadoDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.TipoProjetoDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.MoldeDisplay.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.CaminhoPastaServidorDisplay.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        _projetosFiltrados.Clear();
        _projetosFiltrados.AddRange(query.OrderByDescending(item => item.Projeto_id).ThenBy(item => item.NomeProjetoDisplay));

        var totalFiltrado = _projetosFiltrados.Count;
        var totalPaginas = totalFiltrado <= 0 ? 1 : (int)Math.Ceiling((double)totalFiltrado / PageSize);
        UpdatePagination(totalFiltrado, totalPaginas);

        var paginaAtual = _projetosFiltrados
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        Projetos.Clear();
        foreach (var projeto in paginaAtual)
            Projetos.Add(projeto);

        OnPropertyChanged(nameof(HasProjetos));
        OnPropertyChanged(nameof(TotalProjetos));
        OnPropertyChanged(nameof(TotalMoldesAssociados));
        OnPropertyChanged(nameof(EmptyMessage));
    }
}
