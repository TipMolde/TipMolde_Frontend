using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class AdicionarProjetoViewModel : ObservableObject
{
    private readonly MoldesService _moldesService;
    private readonly ProjetosService _projetosService;
    private readonly AuthorizationService _authorizationService;
    private readonly IDialogService _dialogService;
    private bool _permissionsLoaded;

    public AdicionarProjetoViewModel(
        MoldesService moldesService,
        ProjetosService projetosService,
        AuthorizationService authorizationService,
        IDialogService dialogService)
    {
        _moldesService = moldesService;
        _projetosService = projetosService;
        _authorizationService = authorizationService;
        _dialogService = dialogService;
    }

    public ObservableCollection<MoldeDto> Moldes { get; } = new();
    public ObservableCollection<MoldeDto> MoldesFiltrados { get; } = new();
    public ObservableCollection<TipoProjetoOption> TipoProjetoOptions { get; } = new();

    [ObservableProperty]
    private MoldeDto? selectedMolde;

    [ObservableProperty]
    private string moldeSearchTerm = string.Empty;

    [ObservableProperty]
    private TipoProjetoOption? selectedTipoProjetoOption;

    [ObservableProperty]
    private string nomeProjeto = string.Empty;

    [ObservableProperty]
    private string softwareUtilizado = string.Empty;

    [ObservableProperty]
    private string caminhoPastaServidor = string.Empty;

    [ObservableProperty]
    private bool isAdmin;

    [ObservableProperty]
    private bool isLoadingData;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool CanCreate => IsAdmin
                             && !IsSaving
                             && !IsLoadingData
                             && SelectedMolde is not null
                             && !string.IsNullOrWhiteSpace(NomeProjeto)
                             && !string.IsNullOrWhiteSpace(SoftwareUtilizado)
                             && !string.IsNullOrWhiteSpace(CaminhoPastaServidor)
                             && SelectedTipoProjetoOption is not null;

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnMoldeSearchTermChanged(string value) => AplicarFiltroMoldes();
    partial void OnNomeProjetoChanged(string value) => UpdateCanCreateState();
    partial void OnSoftwareUtilizadoChanged(string value) => UpdateCanCreateState();
    partial void OnCaminhoPastaServidorChanged(string value) => UpdateCanCreateState();
    partial void OnSelectedMoldeChanged(MoldeDto? value) => UpdateCanCreateState();
    partial void OnSelectedTipoProjetoOptionChanged(TipoProjetoOption? value) => UpdateCanCreateState();
    partial void OnIsAdminChanged(bool value) => UpdateCanCreateState();
    partial void OnIsLoadingDataChanged(bool value) => UpdateCanCreateState();
    partial void OnIsSavingChanged(bool value) => UpdateCanCreateState();

    public async Task LoadAsync()
    {
        if (_permissionsLoaded && Moldes.Count > 0)
            return;

        IsLoadingData = true;
        ErrorMessage = string.Empty;

        try
        {
            await EnsurePermissionsLoadedAsync(forceRefresh: true);

            TipoProjetoOptions.Clear();
            foreach (var option in GetTipoProjetoOptions())
                TipoProjetoOptions.Add(option);

            if (SelectedTipoProjetoOption is null)
                SelectedTipoProjetoOption = TipoProjetoOptions.FirstOrDefault();

            Moldes.Clear();
            var moldes = await GetAllMoldesAsync();
            foreach (var molde in moldes.OrderBy(item => item.Numero).ThenBy(item => item.Nome))
                Moldes.Add(molde);

            AplicarFiltroMoldes(forceSelectFirst: true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Moldes.Clear();
            MoldesFiltrados.Clear();
            SelectedMolde = null;
        }
        finally
        {
            IsLoadingData = false;
        }
    }

    [RelayCommand]
    private async Task VoltarAsync()
    {
        await ShellNavigationService.GoBackAsync();
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateAsync()
    {
        if (!CanCreate || SelectedMolde is null || SelectedTipoProjetoOption is null)
            return;

        IsSaving = true;
        ErrorMessage = string.Empty;

        try
        {
            var created = await _projetosService.CreateAsync(new CreateProjetoRequest
            {
                NomeProjeto = NomeProjeto,
                SoftwareUtilizado = SoftwareUtilizado,
                TipoProjeto = SelectedTipoProjetoOption.Value,
                CaminhoPastaServidor = CaminhoPastaServidor,
                MoldeId = SelectedMolde.MoldeId
            });

            await _dialogService.ShowSuccessAsync(
                "Projeto criado",
                $"O projeto {created?.NomeProjetoDisplay ?? NomeProjeto.Trim()} foi criado com sucesso.");

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

    private void UpdateCanCreateState()
    {
        OnPropertyChanged(nameof(CanCreate));
        CreateCommand.NotifyCanExecuteChanged();
    }

    private async Task EnsurePermissionsLoadedAsync(bool forceRefresh = false)
    {
        if (_permissionsLoaded && !forceRefresh)
            return;

        await _authorizationService.GetCurrentRoleAsync(forceRefresh);
        IsAdmin = _authorizationService.CanCreateMachines();
        _permissionsLoaded = true;

        if (!IsAdmin)
            ErrorMessage = "Nao tens permissao para criar projetos.";
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

    private void AplicarFiltroMoldes(bool forceSelectFirst = false)
    {
        var termo = NormalizeSearchTerm(MoldeSearchTerm);
        var selectedMoldeId = SelectedMolde?.MoldeId;

        IEnumerable<MoldeDto> moldesFiltrados = Moldes;

        if (!string.IsNullOrWhiteSpace(termo))
        {
            moldesFiltrados = moldesFiltrados.Where(molde =>
                MatchesSearch(molde.DisplayName, termo) ||
                MatchesSearch(molde.Numero, termo) ||
                MatchesSearch(molde.Nome, termo) ||
                MatchesSearch(molde.NumeroMoldeCliente, termo));
        }

        var listaFiltrada = moldesFiltrados.ToList();

        MoldesFiltrados.Clear();
        foreach (var molde in listaFiltrada)
            MoldesFiltrados.Add(molde);

        if (listaFiltrada.Count == 0)
        {
            SelectedMolde = null;
            return;
        }

        if (forceSelectFirst)
        {
            SelectedMolde = listaFiltrada.First();
            return;
        }

        if (selectedMoldeId.HasValue)
        {
            SelectedMolde = listaFiltrada.FirstOrDefault(molde => molde.MoldeId == selectedMoldeId.Value)
                ?? listaFiltrada.First();
            return;
        }

        SelectedMolde = listaFiltrada.First();
    }

    private static bool MatchesSearch(string? value, string normalizedTerm)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(normalizedTerm))
            return false;

        return NormalizeSearchTerm(value).Contains(normalizedTerm, StringComparison.Ordinal);
    }

    private static string NormalizeSearchTerm(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
                builder.Append(char.ToUpperInvariant(character));
        }

        return builder.ToString();
    }

    private static IReadOnlyList<TipoProjetoOption> GetTipoProjetoOptions()
    {
        return
        [
            new TipoProjetoOption("PROJETO_2D", "Projeto 2D"),
            new TipoProjetoOption("PROJETO_3D", "Projeto 3D")
        ];
    }
}

public sealed record TipoProjetoOption(string Value, string DisplayName);
