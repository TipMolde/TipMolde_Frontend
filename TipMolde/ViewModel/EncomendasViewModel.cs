using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

public partial class EncomendasViewModel : PaginatedViewModel
{
    private readonly EncomendasService _encomendasService;

    public EncomendasViewModel(EncomendasService encomendasService)
    {
        _encomendasService = encomendasService;
    }

    public ObservableCollection<EncomendaResumoDto> Encomendas { get; } = new();

    public bool HasEncomendas => Encomendas.Count > 0;
    public string EmptyMessage => "Nao existem encomendas nao concluidas para apresentar.";

    public async Task LoadEncomendasAsync()
    {
        await ReloadCurrentPageAsync();
    }

    protected override async Task LoadPageAsync()
    {
        ErrorMessage = string.Empty;

        await ExecutePagedLoadAsync(async () =>
        {
            var result = await _encomendasService.GetEncomendasNaoConcluidasAsync(Page, PageSize);

            if (result is null)
            {
                ErrorMessage = "Nao foi possivel carregar as encomendas nao concluidas.";
                Encomendas.Clear();
                UpdatePagination(0, 1);
                OnPropertyChanged(nameof(HasEncomendas));
                return;
            }

            Encomendas.Clear();

            foreach (var encomenda in result.Items)
                Encomendas.Add(encomenda);

            UpdatePagination(result.TotalItems, result.TotalPages);
            OnPropertyChanged(nameof(HasEncomendas));
        });
    }

    [RelayCommand]
    private async Task AbrirEncomendaAsync(EncomendaResumoDto? encomenda)
    {
        if (encomenda is null || encomenda.Encomenda_id <= 0)
            return;

        await Shell.Current.GoToAsync(
            $"{nameof(EncomendaDetalhePage)}?encomenda_id={encomenda.Encomenda_id}");
    }
}
