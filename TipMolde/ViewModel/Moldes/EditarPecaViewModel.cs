using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere a edicao de uma peca existente associada a um molde.
/// </summary>
public partial class EditarPecaViewModel : ObservableObject
{
    private readonly PecasService _pecasService;
    private readonly FasesProducaoService _fasesProducaoService;
    private readonly AuthorizationService _authorizationService;
    private readonly IDialogService _dialogService;
    private bool _permissionsLoaded;

    /// <summary>
    /// Construtor do view model de edicao de pecas.
    /// </summary>
    /// <param name="pecasService">Servico usado para carregar e guardar a peca.</param>
    /// <param name="fasesProducaoService">Servico usado para carregar fases de producao disponiveis.</param>
    /// <param name="authorizationService">Servico usado para validar permissoes de gestao de pecas.</param>
    /// <param name="dialogService">Servico usado para apresentar feedback ao utilizador.</param>
    public EditarPecaViewModel(
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
    private int pecaId;

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
    private bool isLoading;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool canManagePieces;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string NumeroMoldeDisplay => string.IsNullOrWhiteSpace(NumeroMolde) ? "Molde sem numero" : NumeroMolde;
    public bool CanSave => CanManagePieces &&
                           PecaId > 0 &&
                           !IsLoading &&
                           !IsSaving &&
                           !string.IsNullOrWhiteSpace(Designacao) &&
                           Prioridade > 0 &&
                           Quantidade > 0;

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnNumeroMoldeChanged(string value) => OnPropertyChanged(nameof(NumeroMoldeDisplay));
    partial void OnCanManagePiecesChanged(bool value) => NotifyCanSaveChanged();

    partial void OnPecaIdChanged(int value) => NotifyCanSaveChanged();

    partial void OnDesignacaoChanged(string value) => NotifyCanSaveChanged();

    partial void OnPrioridadeChanged(int value) => NotifyCanSaveChanged();

    partial void OnQuantidadeChanged(int value) => NotifyCanSaveChanged();

    partial void OnIsLoadingChanged(bool value) => NotifyCanSaveChanged();

    partial void OnIsSavingChanged(bool value) => NotifyCanSaveChanged();

    /// <summary>
    /// Carrega os dados da peca e prepara o formulario de edicao.
    /// </summary>
    /// <param name="pecaId">Identificador da peca a editar.</param>
    /// <param name="numeroMolde">Numero funcional do molde associado.</param>
    /// <returns>Tarefa assincrona da operacao de carregamento.</returns>
    public async Task LoadAsync(int pecaId, string? numeroMolde)
    {
        await EnsurePermissionsLoadedAsync();
        PecaId = pecaId;
        NumeroMolde = numeroMolde?.Trim() ?? string.Empty;
        ErrorMessage = string.Empty;

        if (!CanManagePieces)
        {
            ErrorMessage = "Nao tens permissao para editar pecas.";
            return;
        }

        IsLoading = true;

        try
        {
            await EnsureFasesLoadedAsync();
            var peca = await _pecasService.GetByIdAsync(pecaId);
            if (peca is null)
            {
                ErrorMessage = "Nao foi possivel carregar a peca para edicao.";
                return;
            }

            MoldeId = peca.Molde_id;
            NumeroPeca = peca.NumeroPeca ?? string.Empty;
            Designacao = peca.Designacao ?? string.Empty;
            Prioridade = peca.Prioridade;
            Quantidade = peca.Quantidade;
            Referencia = peca.Referencia ?? string.Empty;
            MaterialDesignacao = peca.MaterialDesignacao ?? string.Empty;
            TratamentoTermico = peca.TratamentoTermico ?? string.Empty;
            Massa = peca.Massa ?? string.Empty;
            Observacao = peca.Observacao ?? string.Empty;
            SelectedProximaFase = FasesProducao.FirstOrDefault(item => item.FasesProducao_id == peca.ProximaFase_id)
                ?? (FasesProducao.Count > 0 ? FasesProducao[0] : null);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private static async Task VoltarAsync()
    {
        await ShellNavigationService.GoBackAsync();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
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
            await _pecasService.UpdateAsync(
                PecaId,
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
                $"A peca {Designacao.Trim()} foi atualizada com sucesso.");

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
            return "Nao tens permissao para editar pecas.";

        if (PecaId <= 0)
            return "Nao foi possivel identificar a peca a editar.";

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
    }

    private void NotifyCanSaveChanged()
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }
}
