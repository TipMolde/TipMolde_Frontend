using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

public partial class ClientesViewModel : SearchableViewModel
{
    private const string SearchModeNome = "Nome";
    private const string SearchModeSigla = "Sigla";

    private readonly ClientesService _clientesService;
    private readonly IDialogService _dialogService;
    private readonly AuthorizationService _authorizationService;
    private bool _permissionsLoaded;

    public ClientesViewModel(
        ClientesService clientesService,
        IDialogService dialogService,
        AuthorizationService authorizationService)
    {
        _clientesService = clientesService;
        _dialogService = dialogService;
        _authorizationService = authorizationService;
    }

    public ObservableCollection<ClienteDto> Clientes { get; } = new();

    public IReadOnlyList<string> SearchModes { get; } = new[]
    {
        SearchModeNome,
        SearchModeSigla
    };

    [ObservableProperty]
    private int selectedSearchModeIndex = -1;

    [ObservableProperty]
    private bool canDeleteClients;

    public void EnsureDefaultSearchMode()
    {
        if (SelectedSearchModeIndex >= 0)
            return;

        SelectedSearchModeIndex = 1;
    }

    public async Task LoadClientesAsync()
    {
        await EnsurePermissionsLoadedAsync();
        await ReloadCurrentPageAsync();
    }

    protected override async Task LoadPageAsync()
    {
        ErrorMessage = string.Empty;

        await ExecutePagedLoadAsync(async () =>
        {
            PagedResult<ClienteDto>? result;

            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                result = await _clientesService.GetClientesAsync(Page, PageSize);
            }
            else if (SelectedSearchModeIndex == 1)
            {
                result = await _clientesService.SearchBySiglaAsync(SearchTerm.Trim(), Page, PageSize);
            }
            else
            {
                result = await _clientesService.SearchByNameAsync(SearchTerm.Trim(), Page, PageSize);
            }

            if (result is null)
            {
                ErrorMessage = "Nao foi possivel carregar os clientes.";
                Clientes.Clear();
                UpdatePagination(0, 1);
                return;
            }

            Clientes.Clear();

            foreach (var cliente in result.Items)
                Clientes.Add(cliente);

            UpdatePagination(result.TotalItems, result.TotalPages);
        });
    }

    [RelayCommand]
    private async Task AbrirDetalheClienteAsync(ClienteDto? cliente)
    {
        if (cliente is null)
            return;

        await Shell.Current.GoToAsync(
            nameof(ClienteDetalhePage),
            true,
            new Dictionary<string, object>
            {
                ["cliente_id"] = cliente.Cliente_id
            });
    }

    [RelayCommand]
    private async Task AbrirAdicionarClienteAsync()
    {
        await Shell.Current.GoToAsync("AdicionarClientePage");
    }

    [RelayCommand]
    private async Task EditarClienteAsync(ClienteDto? cliente)
    {
        if (cliente is null)
            return;

        await Shell.Current.GoToAsync(
            "EditarClientePage",
            true,
            new Dictionary<string, object>
            {
                ["cliente_id"] = cliente.Cliente_id
            });
    }

    [RelayCommand]
    private async Task DeleteAsync(ClienteDto? cliente)
    {
        if (cliente is null || !CanDeleteClients)
            return;

        var confirmar = await _dialogService.ConfirmDeleteAsync(cliente.Nome);

        if (!confirmar)
            return;

        try
        {
            await _clientesService.DeleteAsync(cliente.Cliente_id);

            if (Clientes.Count == 1 && Page > 1)
                Page--;

            await LoadClientesAsync();

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                "O cliente foi eliminado com sucesso.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "Erro",
                ex.Message);
        }
    }

    private async Task EnsurePermissionsLoadedAsync(bool forceRefresh = false)
    {
        if (_permissionsLoaded && !forceRefresh)
            return;

        await _authorizationService.GetCurrentRoleAsync(forceRefresh);
        CanDeleteClients = _authorizationService.CanDeleteClients();
        _permissionsLoaded = true;
    }
}
