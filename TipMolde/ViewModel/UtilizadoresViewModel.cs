using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class UtilizadoresViewModel : ObservableObject
{
    private readonly UtilizadoresService _utilizadoresService;
    private readonly IDialogService _dialogService;

    public UtilizadoresViewModel(
        UtilizadoresService utilizadoresService,
        IDialogService dialogService)
    {
        _utilizadoresService = utilizadoresService;
        _dialogService = dialogService;
    }

    public ObservableCollection<UtilizadorDto> Utilizadores { get; } = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private int page = 1;

    [ObservableProperty]
    private int pageSize = 10;

    [ObservableProperty]
    private int totalPages = 1;

    [ObservableProperty]
    private int totalItems;

    [ObservableProperty]
    private string searchTerm = string.Empty;

    public bool CanGoPrevious => !IsLoading && Page > 1;
    public bool CanGoNext => !IsLoading && Page < TotalPages;
    public bool HasSearch => !string.IsNullOrWhiteSpace(SearchTerm);
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
    }
    partial void OnSearchTermChanged(string value)
    {
        OnPropertyChanged(nameof(HasSearch));
    }

    [RelayCommand]
    public async Task LoadUtilizadoresAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            PagedResult<UtilizadorDto>? result;
            if (string.IsNullOrWhiteSpace(SearchTerm)) 
            {
                result = await _utilizadoresService.GetUtilizadoresAsync(Page, PageSize);
            } else
            {
                result = await _utilizadoresService.SearchAsync(SearchTerm.Trim(), Page, PageSize);
            }

            if (result is null)
            {
                ErrorMessage = "A resposta da API veio vazia.";
                return;
            }

            Utilizadores.Clear();

            foreach (var utilizador in result.Items)
                Utilizadores.Add(utilizador);

            TotalItems = result.TotalItems;
            TotalPages = Math.Max(1, result.TotalPages);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
        }
    }

    [RelayCommand]
    private async Task PesquisarAsync()
    {
        Page = 1;
        await LoadUtilizadoresAsync();
    }

    [RelayCommand]
    private async Task LimparPesquisaAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
            return;

        SearchTerm = string.Empty;
        Page = 1;
        await LoadUtilizadoresAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (!CanGoNext)
            return;

        Page++;
        await LoadUtilizadoresAsync();
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (!CanGoPrevious)
            return;

        Page--;
        await LoadUtilizadoresAsync();
    }

    [RelayCommand]
    private async Task EditarCargoAsync(UtilizadorDto? utilizador)
    {
        if (utilizador is null)
            return;

        await _dialogService.ShowInfoAsync(
            "Editar cargo",
            $"Aqui vais abrir mais tarde a edicao de cargo do utilizador {utilizador.Nome}.");
    }

    [RelayCommand]
    private async Task ReporPasswordAsync(UtilizadorDto? utilizador)
    {
        if (utilizador is null)
            return;

        await _dialogService.ShowInfoAsync(
            "Repor password",
            $"Aqui vais ligar mais tarde a reposicao de password do utilizador {utilizador.Nome}.");
    }

    [RelayCommand]
    private async Task AbrirOpcoesAsync(UtilizadorDto? utilizador)
    {
        if (utilizador is null)
            return;

        var action = await _dialogService.ShowOptionsAsync(utilizador.Nome, "Eliminar utilizador");

        if (action != "Eliminar utilizador")
            return;

        var confirmar = await _dialogService.ConfirmDeleteAsync(utilizador.Nome);

        if (!confirmar)
            return;

        try
        {
            await _utilizadoresService.DeleteUtilizadorAsync(utilizador.Id);

            if (Utilizadores.Count == 1 && Page > 1)
                Page--;

            await LoadUtilizadoresAsync();

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                "O utilizador foi eliminado com sucesso.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "Erro",
                ex.Message);
        }
    }
}