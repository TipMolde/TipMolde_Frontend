using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TipMolde.ViewModel.Defaults;

/// <summary>
/// Fornece comportamento reutilizavel de paginacao para view models de listagem.
/// </summary>
/// <remarks>
/// Centraliza estado de carga, validacao de input de pagina e comandos
/// de navegacao entre paginas para evitar duplicacao na UI.
/// </remarks>
public abstract partial class PaginatedViewModel : ObservableObject
{
    private readonly AsyncRelayCommand _firstPageCommand;
    private readonly AsyncRelayCommand _previousPageCommand;
    private readonly AsyncRelayCommand _goToPageCommand;
    private readonly AsyncRelayCommand _nextPageCommand;
    private readonly AsyncRelayCommand _lastPageCommand;

    /// <summary>
    /// Construtor da base de paginacao.
    /// </summary>
    protected PaginatedViewModel()
    {
        _firstPageCommand = new AsyncRelayCommand(FirstPageAsync, () => CanGoFirst);
        _previousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => CanGoPrevious);
        _goToPageCommand = new AsyncRelayCommand(GoToPageAsync);
        _nextPageCommand = new AsyncRelayCommand(NextPageAsync, () => CanGoNext);
        _lastPageCommand = new AsyncRelayCommand(LastPageAsync, () => CanGoLast);
    }

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
    private string pageInput = "1";

    public bool CanGoFirst => !IsLoading && Page > 1;
    public bool CanGoPrevious => !IsLoading && Page > 1;
    public bool CanGoNext => !IsLoading && Page < TotalPages;
    public bool CanGoLast => !IsLoading && Page < TotalPages;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public IAsyncRelayCommand FirstPageCommand => _firstPageCommand;
    public IAsyncRelayCommand PreviousPageCommand => _previousPageCommand;
    public IAsyncRelayCommand GoToPageCommand => _goToPageCommand;
    public IAsyncRelayCommand NextPageCommand => _nextPageCommand;
    public IAsyncRelayCommand LastPageCommand => _lastPageCommand;

    /// <summary>
    /// Carrega a pagina atual segundo a implementacao concreta do view model derivado.
    /// </summary>
    /// <returns>Tarefa assincrona do carregamento da pagina.</returns>
    protected abstract Task LoadPageAsync();

    /// <summary>
    /// Executa um carregamento paginado protegendo contra concorrencia de pedidos.
    /// </summary>
    /// <param name="loadAction">Acao assincrona que carrega os dados da pagina.</param>
    /// <returns>Tarefa assincrona do carregamento protegido.</returns>
    protected async Task ExecutePagedLoadAsync(Func<Task> loadAction)
    {
        if (IsLoading)
            return;

        IsLoading = true;

        try
        {
            await loadAction();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Atualiza o estado de paginacao exposto pela UI.
    /// </summary>
    /// <param name="totalItems">Total de itens disponiveis no resultado.</param>
    /// <param name="totalPages">Total de paginas calculado para o resultado atual.</param>
    protected void UpdatePagination(int totalItems, int totalPages)
    {
        TotalItems = totalItems;
        TotalPages = Math.Max(1, totalPages);

        if (Page > TotalPages)
            Page = TotalPages;
    }

    /// <summary>
    /// Regressa a primeira pagina e recarrega os dados.
    /// </summary>
    /// <returns>Tarefa assincrona do recarregamento.</returns>
    protected async Task ResetToFirstPageAndReloadAsync()
    {
        Page = 1;
        await LoadPageAsync();
    }

    /// <summary>
    /// Recarrega a pagina atual mantendo o contexto de paginacao.
    /// </summary>
    /// <returns>Tarefa assincrona do recarregamento.</returns>
    protected async Task ReloadCurrentPageAsync()
    {
        await LoadPageAsync();
    }

    private async Task FirstPageAsync()
    {
        if (!CanGoFirst)
            return;

        ErrorMessage = string.Empty;
        Page = 1;
        await LoadPageAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (!CanGoPrevious)
            return;

        ErrorMessage = string.Empty;
        Page--;
        await LoadPageAsync();
    }

    private async Task GoToPageAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(PageInput))
        {
            ErrorMessage = "Tens de indicar uma pagina.";
            return;
        }

        if (!int.TryParse(PageInput.Trim(), out var requestedPage))
        {
            ErrorMessage = "A pagina tem de ser um numero valido.";
            return;
        }

        if (requestedPage < 1)
        {
            ErrorMessage = "A pagina tem de ser maior ou igual a 1.";
            return;
        }

        if (requestedPage > TotalPages)
        {
            ErrorMessage = $"A pagina nao pode ser maior que {TotalPages}.";
            return;
        }

        if (requestedPage == Page)
            return;

        Page = requestedPage;
        await LoadPageAsync();
    }

    private async Task NextPageAsync()
    {
        if (!CanGoNext)
            return;

        ErrorMessage = string.Empty;
        Page++;
        await LoadPageAsync();
    }

    private async Task LastPageAsync()
    {
        if (!CanGoLast)
            return;

        ErrorMessage = string.Empty;
        Page = TotalPages;
        await LoadPageAsync();
    }

    partial void OnIsLoadingChanged(bool value) => NotifyPaginationStateChanged();

    partial void OnPageChanged(int value)
    {
        if (PageInput != value.ToString())
            PageInput = value.ToString();

        NotifyPaginationStateChanged();
    }

    partial void OnTotalPagesChanged(int value) => NotifyPaginationStateChanged();
    partial void OnErrorMessageChanged(string value) => NotifyPaginationStateChanged();

    private void NotifyPaginationStateChanged()
    {
        OnPropertyChanged(nameof(CanGoFirst));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoLast));
        OnPropertyChanged(nameof(HasError));

        _firstPageCommand.NotifyCanExecuteChanged();
        _previousPageCommand.NotifyCanExecuteChanged();
        _nextPageCommand.NotifyCanExecuteChanged();
        _lastPageCommand.NotifyCanExecuteChanged();
    }
}
