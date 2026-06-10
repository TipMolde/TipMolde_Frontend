using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class EditarPecaViewModel : ObservableObject
{
    private readonly PecasService _pecasService;
    private readonly FasesProducaoService _fasesProducaoService;
    private readonly IDialogService _dialogService;

    public EditarPecaViewModel(
        PecasService pecasService,
        FasesProducaoService fasesProducaoService,
        IDialogService dialogService)
    {
        _pecasService = pecasService;
        _fasesProducaoService = fasesProducaoService;
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

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string NumeroMoldeDisplay => string.IsNullOrWhiteSpace(NumeroMolde) ? "Molde sem numero" : NumeroMolde;
    public bool CanSave => PecaId > 0 &&
                           !IsLoading &&
                           !IsSaving &&
                           !string.IsNullOrWhiteSpace(Designacao) &&
                           Prioridade > 0 &&
                           Quantidade > 0;

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnNumeroMoldeChanged(string value) => OnPropertyChanged(nameof(NumeroMoldeDisplay));

    partial void OnPecaIdChanged(int value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnDesignacaoChanged(string value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnPrioridadeChanged(int value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnQuantidadeChanged(int value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSavingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    public async Task LoadAsync(int pecaId, string? numeroMolde)
    {
        PecaId = pecaId;
        NumeroMolde = numeroMolde?.Trim() ?? string.Empty;
        ErrorMessage = string.Empty;
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
                ?? FasesProducao.FirstOrDefault();
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
    private async Task VoltarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (IsSaving)
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
                Designacao,
                Prioridade,
                Quantidade,
                proximaFaseId: SelectedProximaFase?.FasesProducao_id,
                numeroPeca: NumeroPeca,
                referencia: Referencia,
                materialDesignacao: MaterialDesignacao,
                tratamentoTermico: TratamentoTermico,
                massa: Massa,
                observacao: Observacao);

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
}
