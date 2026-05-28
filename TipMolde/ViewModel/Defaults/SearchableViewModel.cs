using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TipMolde.ViewModel.Defaults;

public abstract partial class SearchableViewModel : PaginatedViewModel
{
    private readonly AsyncRelayCommand _pesquisarCommand;
    private readonly AsyncRelayCommand _limparPesquisaCommand;

    protected SearchableViewModel()
    {
        _pesquisarCommand = new AsyncRelayCommand(PesquisarAsync);
        _limparPesquisaCommand = new AsyncRelayCommand(LimparPesquisaAsync, () => HasSearch);
    }

    [ObservableProperty]
    private string searchTerm = string.Empty;

    public bool HasSearch => !string.IsNullOrWhiteSpace(SearchTerm);

    public IAsyncRelayCommand PesquisarCommand => _pesquisarCommand;
    public IAsyncRelayCommand LimparPesquisaCommand => _limparPesquisaCommand;

    private async Task PesquisarAsync()
    {
        await ResetToFirstPageAndReloadAsync();
    }

    private async Task LimparPesquisaAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
            return;

        SearchTerm = string.Empty;
        await ResetToFirstPageAndReloadAsync();
    }

    partial void OnSearchTermChanged(string value)
    {
        OnPropertyChanged(nameof(HasSearch));
        _limparPesquisaCommand.NotifyCanExecuteChanged();
    }
}