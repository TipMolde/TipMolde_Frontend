using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;

namespace TipMolde.ViewModel;

public partial class RelatoriosViewModel : ObservableObject
{
    private const int MoldesPageSize = 100;
    private const int ContextosPageSize = 100;
    private const int FichasPageSize = 200;

    private readonly MoldesService _moldesService;
    private readonly EncomendasService _encomendasService;
    private readonly RelatoriosService _relatoriosService;
    private readonly IDestinationFolderPickerService _destinationFolderPickerService;
    private readonly IDialogService _dialogService;

    private List<FichaProducaoResumoDto> _fichasDoMoldeSelecionado = [];
    private int? _fichasMoldeIdEmCache;
    private bool _catalogoCarregado;
    private string _ultimoTermoCatalogo = string.Empty;
    private CancellationTokenSource? _catalogoReloadCts;

    public RelatoriosViewModel(
        MoldesService moldesService,
        EncomendasService encomendasService,
        RelatoriosService relatoriosService,
        IDestinationFolderPickerService destinationFolderPickerService,
        IDialogService dialogService)
    {
        _moldesService = moldesService;
        _encomendasService = encomendasService;
        _relatoriosService = relatoriosService;
        _destinationFolderPickerService = destinationFolderPickerService;
        _dialogService = dialogService;
    }

    public ObservableCollection<MoldeDto> Moldes { get; } = new();
    public ObservableCollection<EncomendaMoldeDto> ContextosEncomenda { get; } = new();
    public IReadOnlyList<string> TiposRelatorio { get; } = ["FLT", "FRE", "FRM", "FRA", "FOP"];

    [ObservableProperty]
    private bool isLoadingCatalogo;

    [ObservableProperty]
    private bool isLoadingContextos;

    [ObservableProperty]
    private bool isPreviewing;

    [ObservableProperty]
    private bool isGenerating;

    [ObservableProperty]
    private string searchTerm = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private MoldeDto? selectedMolde;

    [ObservableProperty]
    private EncomendaMoldeDto? selectedContexto;

    [ObservableProperty]
    private string selectedTipoRelatorio = "FLT";

    [ObservableProperty]
    private bool hasPreview;

    [ObservableProperty]
    private bool isReportAvailable;

    [ObservableProperty]
    private string previewTitulo = "Sem relatorio gerado";

    [ObservableProperty]
    private string previewResumo = "Seleciona um molde, escolhe o tipo de relatorio e gera o ficheiro final para descarregar.";

    [ObservableProperty]
    private string previewDetalhes = string.Empty;

    [ObservableProperty]
    private string previewOrigem = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSearch => !string.IsNullOrWhiteSpace(SearchTerm);
    public bool HasSelectedMolde => SelectedMolde is not null;
    public bool HasContextos => ContextosEncomenda.Count > 0;
    public bool HasMultipleContextos => ContextosEncomenda.Count > 1;
    public bool IsBusy => IsLoadingCatalogo || IsLoadingContextos || IsPreviewing || IsGenerating;
    public bool CanPreview => SelectedMolde is not null && SelectedContexto is not null && !IsBusy;
    public bool CanGenerate => SelectedMolde is not null && SelectedContexto is not null && !IsBusy;
    public string PreviewButtonText => IsPreviewing ? "A abrir pre-visualizacao..." : "Pre-visualizar";
    public string GenerateButtonText => IsGenerating ? "A gerar e descarregar..." : "Gerar e descarregar";
    public string SelectedMoldeDisplay => SelectedMolde is null
        ? "Nenhum molde selecionado"
        : $"{SelectedMolde.Numero} - {SelectedMolde.Nome}";
    public string SelectedMoldeDescricao => GetSelectedMoldeDescricao();
    public string ContextosHint => HasMultipleContextos
        ? "Este molde existe em mais do que uma encomenda. Escolhe o contexto certo antes de gerar."
        : "O contexto da encomenda sera usado para localizar a FLT e as fichas editaveis relacionadas.";

    partial void OnSearchTermChanged(string value)
    {
        OnPropertyChanged(nameof(HasSearch));
        _ = AgendarRecargaCatalogoAsync(value);
    }

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    partial void OnSelectedMoldeChanged(MoldeDto? value)
    {
        OnPropertyChanged(nameof(HasSelectedMolde));
        OnPropertyChanged(nameof(SelectedMoldeDisplay));
        OnPropertyChanged(nameof(SelectedMoldeDescricao));
        OnPropertyChanged(nameof(CanPreview));
        OnPropertyChanged(nameof(CanGenerate));
        PreviewCommand.NotifyCanExecuteChanged();
        GenerateCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedContextoChanged(EncomendaMoldeDto? value)
    {
        ResetPreviewState();
        OnPropertyChanged(nameof(CanPreview));
        OnPropertyChanged(nameof(CanGenerate));
        PreviewCommand.NotifyCanExecuteChanged();
        GenerateCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedTipoRelatorioChanged(string value)
    {
        ResetPreviewState();
    }

    partial void OnIsLoadingCatalogoChanged(bool value) => NotifyBusyStateChanged();
    partial void OnIsLoadingContextosChanged(bool value) => NotifyBusyStateChanged();
    partial void OnIsPreviewingChanged(bool value)
    {
        OnPropertyChanged(nameof(PreviewButtonText));
        NotifyBusyStateChanged();
    }

    partial void OnIsGeneratingChanged(bool value)
    {
        OnPropertyChanged(nameof(GenerateButtonText));
        NotifyBusyStateChanged();
    }

    public async Task LoadAsync()
    {
        if (_catalogoCarregado && string.Equals(_ultimoTermoCatalogo, SearchTerm.Trim(), StringComparison.Ordinal))
            return;

        await CarregarCatalogoAsync(forceRefresh: true, searchTerm: SearchTerm);
    }

    [RelayCommand]
    private async Task RecarregarAsync()
    {
        await CarregarCatalogoAsync(forceRefresh: true, searchTerm: SearchTerm);
    }

    [RelayCommand]
    private async Task AbrirFopGeralAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            await Shell.Current.GoToAsync(nameof(FopGeralPage));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Relatorios", ex.Message);
        }
    }

    [RelayCommand]
    private void LimparPesquisa()
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
            return;

        SearchTerm = string.Empty;
    }

    [RelayCommand]
    private async Task SelecionarMoldeAsync(MoldeDto? molde)
    {
        if (molde is null)
            return;

        SelectedMolde = molde;
        await CarregarContextosAsync(molde.MoldeId);
    }

    [RelayCommand(CanExecute = nameof(CanPreview))]
    private async Task PreviewAsync()
    {
        if (SelectedMolde is null || SelectedContexto is null)
            return;

        ErrorMessage = string.Empty;
        IsPreviewing = true;

        try
        {
            var request = await ResolverPedidoAsync();
            if (request is null)
                return;

            HasPreview = true;
            IsReportAvailable = true;
            PreviewTitulo = $"Relatorio {request.TipoRelatorio} pronto a gerar";
            PreviewResumo = $"O contexto do relatorio foi validado para o molde {SelectedMolde.Numero}. Se estiver tudo certo, podes gerar o ficheiro definitivo a seguir.";
            PreviewOrigem = "Contexto validado sem gerar ficheiro.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Erro", ex.Message);
        }
        finally
        {
            IsPreviewing = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync()
    {
        if (SelectedMolde is null || SelectedContexto is null)
            return;

        ErrorMessage = string.Empty;

        try
        {
            var destinationFolder = await _destinationFolderPickerService.PickFolderAsync("Escolher destino do relatorio");
            if (string.IsNullOrWhiteSpace(destinationFolder))
                return;

            var request = await ResolverPedidoAsync();
            if (request is null)
                return;

            IsGenerating = true;
            var generated = await _relatoriosService.GenerateAsync(request, destinationFolder);

            HasPreview = true;
            IsReportAvailable = true;
            PreviewTitulo = $"Relatorio {request.TipoRelatorio} gerado";
            PreviewResumo = $"O relatorio foi guardado com sucesso para o molde {SelectedMolde.Numero}.";
            PreviewOrigem = $"Ficheiro guardado em: {generated.FilePath}";

            await _dialogService.ShowSuccessAsync(
                "Relatorio gerado",
                $"O relatorio {request.TipoRelatorio} foi gerado com sucesso em {generated.FilePath}.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Erro", ex.Message);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private async Task CarregarCatalogoAsync(bool forceRefresh = false, string? searchTerm = null)
    {
        if (IsLoadingCatalogo)
            return;

        IsLoadingCatalogo = true;
        ErrorMessage = string.Empty;

        try
        {
            var termoPesquisa = string.IsNullOrWhiteSpace(searchTerm)
                ? string.Empty
                : searchTerm.Trim();

            if (!forceRefresh && _catalogoCarregado && string.Equals(_ultimoTermoCatalogo, termoPesquisa, StringComparison.Ordinal))
                return;

            var paginaAtual = 1;
            var totalPaginas = 1;
            var moldes = new List<MoldeDto>();

            do
            {
                var pagina = await _moldesService.GetComEncomendaAsync(termoPesquisa, paginaAtual, MoldesPageSize);
                if (pagina is null)
                {
                    ErrorMessage = "Nao foi possivel carregar a lista de moldes com encomenda.";
                    return;
                }

                moldes.AddRange(pagina.Items);
                totalPaginas = Math.Max(1, pagina.TotalPages);
                paginaAtual++;
            }
            while (paginaAtual <= totalPaginas);

            Moldes.Clear();
            foreach (var molde in moldes)
                Moldes.Add(molde);

            _ultimoTermoCatalogo = termoPesquisa;
            _catalogoCarregado = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoadingCatalogo = false;
        }
    }

    private async Task CarregarContextosAsync(int moldeId)
    {
        if (IsLoadingContextos)
            return;

        IsLoadingContextos = true;
        ErrorMessage = string.Empty;
        ResetPreviewState();
        ContextosEncomenda.Clear();
        _fichasDoMoldeSelecionado.Clear();
        _fichasMoldeIdEmCache = null;

        try
        {
            var paginaAtual = 1;
            var totalPaginas = 1;
            var contextos = new List<EncomendaMoldeDto>();

            do
            {
                var pagina = await _encomendasService.GetEncomendaMoldesByMoldeIdAsync(moldeId, paginaAtual, ContextosPageSize);
                if (pagina is null)
                {
                    ErrorMessage = "Nao foi possivel carregar os contextos de encomenda deste molde.";
                    return;
                }

                contextos.AddRange(pagina.Items);
                totalPaginas = Math.Max(1, pagina.TotalPages);
                paginaAtual++;
            }
            while (paginaAtual <= totalPaginas);

            foreach (var contexto in contextos
                         .OrderBy(item => item.Prioridade)
                         .ThenBy(item => item.DataEntregaPrevista)
                         .ThenByDescending(item => item.EncomendaMolde_id))
            {
                ContextosEncomenda.Add(contexto);
            }

            if (ContextosEncomenda.Count > 0)
                SelectedContexto = ContextosEncomenda.First();
            else
            {
                PreviewTitulo = "Sem contexto comercial";
                PreviewResumo = "O molde selecionado ainda nao tem nenhuma associacao Encomenda-Molde, por isso nao e possivel gerar relatorios.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            OnPropertyChanged(nameof(HasContextos));
            OnPropertyChanged(nameof(HasMultipleContextos));
            OnPropertyChanged(nameof(ContextosHint));
            IsLoadingContextos = false;
        }
    }

    private async Task AgendarRecargaCatalogoAsync(string searchTerm)
    {
        _catalogoReloadCts?.Cancel();
        _catalogoReloadCts?.Dispose();
        var cts = new CancellationTokenSource();
        _catalogoReloadCts = cts;

        try
        {
            await Task.Delay(300, cts.Token);
            while (IsLoadingCatalogo)
                await Task.Delay(100, cts.Token);

            await CarregarCatalogoAsync(forceRefresh: true, searchTerm: searchTerm);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(_catalogoReloadCts, cts))
                _catalogoReloadCts = null;

            cts.Dispose();
        }
    }

    private async Task<RelatorioExportRequest?> ResolverPedidoAsync()
    {
        if (SelectedMolde is null || SelectedContexto is null)
            return null;

        var tipo = SelectedTipoRelatorio.ToUpperInvariant();

        if (tipo == "FLT")
        {
            IsReportAvailable = true;
            PreviewTitulo = "FLT pronta a exportar";
            PreviewResumo = $"Vai ser usada a associacao comercial {SelectedContexto.ContextoDisplay} para gerar a FLT.";
            PreviewDetalhes = BuildPreviewDetails(null);
            PreviewOrigem = $"Contexto comercial: {SelectedContexto.ContextoDisplay}";

            return new RelatorioExportRequest
            {
                TipoRelatorio = tipo,
                MoldeId = SelectedMolde.MoldeId,
                NumeroMolde = SelectedMolde.Numero,
                EncomendaMoldeId = SelectedContexto.EncomendaMolde_id
            };
        }

        var resultadoFicha = await ObterOuCriarFichaDoContextoAsync(tipo, SelectedContexto.EncomendaMolde_id);
        if (resultadoFicha is null)
            return null;

        var (ficha, criadaAgora) = resultadoFicha.Value;

        IsReportAvailable = true;
        PreviewTitulo = criadaAgora
            ? $"{tipo} criada e pronta a exportar"
            : $"{tipo} pronta a exportar";
        PreviewResumo = criadaAgora
            ? $"Nao existia nenhuma ficha {tipo} para este contexto. O backend criou uma nova e ela ficou pronta para exportacao."
            : $"Foi localizada a ficha {tipo} mais recente para este contexto.";
        PreviewDetalhes = BuildPreviewDetails(ficha);
        PreviewOrigem = criadaAgora
            ? $"Ficha criada em {ficha.DataCriacao:dd/MM/yyyy HH:mm}"
            : $"Ficha encontrada em {ficha.DataCriacao:dd/MM/yyyy HH:mm}";

        return new RelatorioExportRequest
        {
            TipoRelatorio = tipo,
            MoldeId = SelectedMolde.MoldeId,
            NumeroMolde = SelectedMolde.Numero,
            EncomendaMoldeId = SelectedContexto.EncomendaMolde_id,
            FichaProducaoId = ficha.FichaProducaoId
        };
    }

    private async Task<IReadOnlyList<FichaProducaoResumoDto>> ObterFichasDoMoldeSelecionadoAsync()
    {
        if (SelectedMolde is null)
            return [];

        if (_fichasMoldeIdEmCache == SelectedMolde.MoldeId)
            return _fichasDoMoldeSelecionado;

        var paginaAtual = 1;
        var totalPaginas = 1;
        var fichas = new List<FichaProducaoResumoDto>();

        do
        {
            var pagina = await _relatoriosService.GetFichasByMoldeIdAsync(SelectedMolde.MoldeId, paginaAtual, FichasPageSize);
            if (pagina is null)
                break;

            fichas.AddRange(pagina.Items);
            totalPaginas = Math.Max(1, pagina.TotalPages);
            paginaAtual++;
        }
        while (paginaAtual <= totalPaginas);

        _fichasDoMoldeSelecionado = fichas;
        _fichasMoldeIdEmCache = SelectedMolde.MoldeId;

        return _fichasDoMoldeSelecionado;
    }

    private async Task<(FichaProducaoResumoDto Ficha, bool CriadaAgora)?> ObterOuCriarFichaDoContextoAsync(string tipoRelatorio, int encomendaMoldeId)
    {
        var fichas = await ObterFichasDoMoldeSelecionadoAsync();
        var fichaExistente = fichas
            .Where(item => item.EncomendaMoldeId == encomendaMoldeId &&
                           string.Equals(item.TipoDisplay, tipoRelatorio, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.DataCriacao)
            .FirstOrDefault();

        if (fichaExistente is not null)
            return (fichaExistente, false);

        var fichaCriada = await _relatoriosService.EnsureFichaAsync(tipoRelatorio, encomendaMoldeId);
        if (fichaCriada is null)
            return null;

        if (_fichasMoldeIdEmCache == SelectedMolde?.MoldeId)
        {
            _fichasDoMoldeSelecionado.RemoveAll(item => item.FichaProducaoId == fichaCriada.FichaProducaoId);
            _fichasDoMoldeSelecionado.Add(fichaCriada);
        }

        return (fichaCriada, true);
    }

    private string BuildPreviewDetails(FichaProducaoResumoDto? ficha)
    {
        if (SelectedMolde is null || SelectedContexto is null)
            return string.Empty;

        var linhas = new List<string>
        {
            $"Molde: {SelectedMolde.Numero} - {SelectedMolde.Nome}",
            $"Tipo: {SelectedTipoRelatorio}",
            $"Contexto comercial: {SelectedContexto.ContextoDisplay}",
            $"Encomenda cliente: {SelectedContexto.NumeroEncomendaClienteDisplay}",
            $"Entrega prevista: {SelectedContexto.DataEntregaPrevistaDisplay}",
            $"Prioridade: {SelectedContexto.Prioridade}"
        };

        if (ficha is not null)
            linhas.Add($"Ficha usada em {ficha.DataCriacao:dd/MM/yyyy HH:mm}");

        return string.Join(Environment.NewLine, linhas);
    }

    private string GetSelectedMoldeDescricao()
    {
        if (SelectedMolde is null)
            return "Escolhe um molde na lista para carregar os contextos de encomenda e os relatorios disponiveis.";

        if (string.IsNullOrWhiteSpace(SelectedMolde.Descricao))
            return "Sem descricao disponivel.";

        return SelectedMolde.Descricao;
    }

    private void ResetPreviewState()
    {
        HasPreview = false;
        IsReportAvailable = false;
        PreviewTitulo = "Sem relatorio gerado";
        PreviewResumo = "Seleciona um molde, escolhe o tipo de relatorio e gera o ficheiro final para descarregar.";
        PreviewDetalhes = string.Empty;
        PreviewOrigem = string.Empty;
    }

    private void NotifyBusyStateChanged()
    {
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanPreview));
        OnPropertyChanged(nameof(CanGenerate));
        OnPropertyChanged(nameof(PreviewButtonText));
        OnPropertyChanged(nameof(GenerateButtonText));
        PreviewCommand.NotifyCanExecuteChanged();
        GenerateCommand.NotifyCanExecuteChanged();
    }

}
