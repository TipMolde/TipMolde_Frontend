using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

public partial class EncomendasViewModel : SearchableViewModel
{
    private readonly EncomendasService _encomendasService;

    public EncomendasViewModel(EncomendasService encomendasService)
    {
        _encomendasService = encomendasService;
    }

    public ObservableCollection<EncomendaResumoDto> Encomendas { get; } = new();

    public bool HasEncomendas => Encomendas.Count > 0;
    public string EmptyMessage => string.IsNullOrWhiteSpace(SearchTerm)
        ? "Nao existem encomendas ativas para apresentar."
        : "Nenhuma encomenda corresponde a pesquisa atual.";

    public async Task LoadEncomendasAsync()
    {
        await ReloadCurrentPageAsync();
    }

    [RelayCommand]
    private async Task AbrirAdicionarEncomendaAsync()
    {
        await Shell.Current.GoToAsync(nameof(AdicionarEncomendaPage));
    }

    [RelayCommand]
    private async Task AbrirAdicionarMoldeAsync()
    {
        await Shell.Current.GoToAsync(nameof(AdicionarMoldePage));
    }

    protected override async Task LoadPageAsync()
    {
        ErrorMessage = string.Empty;

        await ExecutePagedLoadAsync(async () =>
        {
            var result = string.IsNullOrWhiteSpace(SearchTerm)
                ? await _encomendasService.GetEncomendasNaoConcluidasAsync(Page, PageSize)
                : await _encomendasService.SearchEncomendasNaoConcluidasAsync(SearchTerm.Trim(), Page, PageSize);

            if (result is null)
            {
                ErrorMessage = "Nao foi possivel carregar as encomendas ativas.";
                return;
            }

            Encomendas.Clear();

            foreach (var encomenda in result.Items)
                Encomendas.Add(encomenda);

            UpdatePagination(result.TotalItems, result.TotalPages);
            OnPropertyChanged(nameof(HasEncomendas));
            OnPropertyChanged(nameof(EmptyMessage));
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
