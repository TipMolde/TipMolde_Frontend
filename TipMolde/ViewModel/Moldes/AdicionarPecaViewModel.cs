using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere o formulario de criacao manual de pecas associadas a um molde.
/// </summary>
public partial class AdicionarPecaViewModel : ObservableObject
{
    private readonly PecasService _pecasService;
    private readonly FasesProducaoService _fasesProducaoService;
    private readonly AuthorizationService _authorizationService;
    private readonly IDialogService _dialogService;
    private bool _permissionsLoaded;

    /// <summary>
    /// Construtor do view model de criacao de pecas.
    /// </summary>
    /// <param name="pecasService">Servico usado para persistir a nova peca.</param>
    /// <param name="fasesProducaoService">Servico usado para carregar fases de producao selecionaveis.</param>
    /// <param name="authorizationService">Servico usado para validar permissoes de gestao de pecas.</param>
    /// <param name="dialogService">Servico usado para apresentar feedback ao utilizador.</param>
    public AdicionarPecaViewModel(
        PecasService pecasService,
        FasesProducaoService fasesProducaoService,
        AuthorizationService authorizationService,
        IDialogService dialogService)
    {
        _pecasService = pecasService;
        _fasesProducaoService = fasesProducaoService;
        _authorizationService = authorizationService;
        _dialogService = dialogService;
    }

    public ObservableCollection<FaseProducaoItem> FasesProducao { get; } = new();

    [ObservableProperty]
    private int moldeId;

    [ObservableProperty]
    private string numeroMolde = string.Empty;

    [ObservableProperty]
    private string numeroPeca = string.Empty;

    [ObservableProperty]
    private string designacao = string.Empty;

    [ObservableProperty]
    private int prioridade = 1;

    [ObservableProperty]
    private int quantidade = 1;

    [ObservableProperty]
    private string referencia = string.Empty;

    [ObservableProperty]
    private string materialDesignacao = string.Empty;

    [ObservableProperty]
    private string tratamentoTermico = string.Empty;

    [ObservableProperty]
    private string massa = string.Empty;

    [ObservableProperty]
    private string observacao = string.Empty;

    [ObservableProperty]
    private FaseProducaoItem? selectedProximaFase;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool canManagePieces;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string NumeroMoldeDisplay => string.IsNullOrWhiteSpace(NumeroMolde) ? "Molde sem numero" : NumeroMolde;
    public bool CanCreate => CanManagePieces &&
                             MoldeId > 0 &&
                             !IsSaving &&
                             !string.IsNullOrWhiteSpace(Designacao) &&
                             Prioridade > 0 &&
                             Quantidade > 0;

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnCanManagePiecesChanged(bool value) => NotifyCanCreateStateChanged();

    partial void OnMoldeIdChanged(int value) => NotifyCanCreateStateChanged();

    partial void OnNumeroMoldeChanged(string value) => OnPropertyChanged(nameof(NumeroMoldeDisplay));

    partial void OnDesignacaoChanged(string value) => NotifyCanCreateStateChanged();

    partial void OnPrioridadeChanged(int value) => NotifyCanCreateStateChanged();

    partial void OnQuantidadeChanged(int value) => NotifyCanCreateStateChanged();

    partial void OnIsSavingChanged(bool value) => NotifyCanCreateStateChanged();

    /// <summary>
    /// Inicializa o formulario com o molde de destino e carrega o contexto auxiliar.
    /// </summary>
    /// <param name="moldeId">Identificador do molde onde a peca sera criada.</param>
    /// <param name="numeroMolde">Numero funcional do molde.</param>
    /// <returns>Tarefa assincrona da operacao de carregamento.</returns>
    public async Task LoadAsync(int moldeId, string? numeroMolde)
    {
        await EnsurePermissionsLoadedAsync();
        MoldeId = moldeId;
        NumeroMolde = numeroMolde?.Trim() ?? string.Empty;
        ErrorMessage = string.Empty;

        if (!CanManagePieces)
        {
            ErrorMessage = "Nao tens permissao para adicionar pecas.";
            return;
        }

        await EnsureFasesLoadedAsync();
    }

    [RelayCommand]
    private static async Task VoltarAsync()
    {
        await ShellNavigationService.GoBackAsync();
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateAsync()
    {
        if (IsSaving || !CanManagePieces)
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
            await _pecasService.CreateAsync(
                MoldeId,
                new PecaUpsertRequest
                {
                    Designacao = Designacao,
                    Prioridade = Prioridade,
                    Quantidade = Quantidade,
                    ProximaFaseId = SelectedProximaFase?.FasesProducao_id,
                    NumeroPeca = NumeroPeca,
                    Referencia = Referencia,
                    MaterialDesignacao = MaterialDesignacao,
                    TratamentoTermico = TratamentoTermico,
                    Massa = Massa,
                    Observacao = Observacao
                });

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                $"A peca {Designacao.Trim()} foi criada no molde {NumeroMoldeDisplay}.");

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

    private string BuildValidationMessage()
    {
        if (!CanManagePieces)
            return "Nao tens permissao para adicionar pecas.";

        if (MoldeId <= 0)
            return "Nao foi possivel identificar o molde para criar a peca.";

        if (string.IsNullOrWhiteSpace(Designacao))
            return "Indique a designacao da peca.";

        if (Prioridade <= 0)
            return "Indique uma prioridade valida.";

        if (Quantidade <= 0)
            return "Indique uma quantidade valida.";

        return string.Empty;
    }

    private async Task EnsurePermissionsLoadedAsync(bool forceRefresh = false)
    {
        if (_permissionsLoaded && !forceRefresh)
            return;

        await _authorizationService.GetCurrentRoleAsync(forceRefresh);
        CanManagePieces = _authorizationService.CanManagePieces();
        _permissionsLoaded = true;
    }

    private async Task EnsureFasesLoadedAsync()
    {
        if (FasesProducao.Count > 0)
            return;

        var pagina = await _fasesProducaoService.GetAllAsync(1, 100);
        if (pagina?.Items is null)
            return;

        foreach (var fase in pagina.Items.OrderBy(item => item.FasesProducao_id))
            FasesProducao.Add(fase);

        SelectedProximaFase = FasesProducao.First();
    }

    private void NotifyCanCreateStateChanged()
    {
        OnPropertyChanged(nameof(CanCreate));
        CreateCommand.NotifyCanExecuteChanged();
    }
}
