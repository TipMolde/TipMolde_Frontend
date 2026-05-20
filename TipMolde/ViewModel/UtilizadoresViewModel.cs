using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;

namespace TipMolde.ViewModel;

public partial class UtilizadoresViewModel : ObservableObject
{
    private readonly UtilizadoresService _utilizadoresService;

    public UtilizadoresViewModel(UtilizadoresService utilizadoresService)
    {
        _utilizadoresService = utilizadoresService;
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

    public bool CanGoPrevious => !IsLoading && Page > 1;
    public bool CanGoNext => !IsLoading && Page < TotalPages;

    [RelayCommand]
    public async Task LoadUtilizadoresAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _utilizadoresService.GetUtilizadoresAsync(Page, PageSize);

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
}