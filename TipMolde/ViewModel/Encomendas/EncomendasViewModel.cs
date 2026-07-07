using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere a listagem principal de encomendas ativas.
/// </summary>
public partial class EncomendasViewModel : SearchableViewModel
{
    private readonly EncomendasService _encomendasService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Construtor do view model de encomendas.
    /// </summary>
    /// <param name="encomendasService">Servico usado para consultar encomendas.</param>
    /// <param name="navigationService">Servico de navegacao principal da aplicacao.</param>
    public EncomendasViewModel(
        EncomendasService encomendasService,
        INavigationService navigationService)
    {
        _encomendasService = encomendasService;
        _navigationService = navigationService;
    }

    public ObservableCollection<EncomendaResumoDto> Encomendas { get; } = new();

    public bool HasEncomendas => Encomendas.Count > 0;
    public string EmptyMessage => string.IsNullOrWhiteSpace(SearchTerm)
        ? "Nao existem encomendas ativas para apresentar."
        : "Nenhuma encomenda corresponde a pesquisa atual.";

    /// <summary>
    /// Carrega a lista de encomendas ativas.
    /// </summary>
    /// <returns>Tarefa assincrona do carregamento da lista.</returns>
    public async Task LoadEncomendasAsync()
    {
        await ReloadCurrentPageAsync();
    }

    /// <summary>
    /// Abre o formulario de criacao de encomenda.
    /// </summary>
    /// <returns>Tarefa assincrona da navegacao para criacao.</returns>
    [RelayCommand]
    private async Task AbrirAdicionarEncomendaAsync()
    {
        await _navigationService.GoToAsync(nameof(AdicionarEncomendaPage));
    }

    /// <summary>
    /// Abre o formulario de criacao de molde.
    /// </summary>
    /// <returns>Tarefa assincrona da navegacao para criacao.</returns>
    [RelayCommand]
    private async Task AbrirAdicionarMoldeAsync()
    {
        await _navigationService.GoToAsync(nameof(AdicionarMoldePage));
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

    /// <summary>
    /// Abre o detalhe da encomenda selecionada.
    /// </summary>
    /// <param name="encomenda">Encomenda escolhida pelo utilizador.</param>
    /// <returns>Tarefa assincrona da navegacao para o detalhe.</returns>
    [RelayCommand]
    private async Task AbrirEncomendaAsync(EncomendaResumoDto? encomenda)
    {
        if (encomenda is null || encomenda.Encomenda_id <= 0)
            return;

        await _navigationService.GoToAsync(
            $"{nameof(EncomendaDetalhePage)}?encomenda_id={encomenda.Encomenda_id}");
    }
}
