using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class RelatoriosViewModel : ObservableObject
{
    private const int MoldesPageSize = 100;
    private const int ContextosPageSize = 100;
    private const int FichasPageSize = 200;

    private readonly MoldesService _moldesService;
    private readonly EncomendasService _encomendasService;
    private readonly RelatoriosService _relatoriosService;
    private readonly IDialogService _dialogService;

    private readonly List<MoldeDto> _catalogoCompleto = [];
    private List<FichaProducaoResumoDto> _fichasDoMoldeSelecionado = [];
    private int? _fichasMoldeIdEmCache;
    private bool _catalogoCarregado;

    public RelatoriosViewModel(
        MoldesService moldesService,
        EncomendasService encomendasService,
        RelatoriosService relatoriosService,
        IDialogService dialogService)
    {
        _moldesService = moldesService;
        _encomendasService = encomendasService;
        _relatoriosService = relatoriosService;
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
    private string previewTitulo = "Sem pre-visualizacao";

    [ObservableProperty]
    private string previewResumo = "Seleciona um molde, escolhe o tipo de relatorio e usa a pre-visualizacao para validar o contexto antes de gerar.";

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
    public string GenerateButtonText => IsGenerating ? "A gerar relatorio..." : "Gerar relatorio";
    public string SelectedMoldeDisplay => SelectedMolde is null
        ? "Nenhum molde selecionado"
        : $"{SelectedMolde.Numero} - {SelectedMolde.Nome}";
    public string SelectedMoldeDescricao => SelectedMolde is null
        ? "Escolhe um molde na lista para carregar os contextos de encomenda e os relatorios disponiveis."
        : string.IsNullOrWhiteSpace(SelectedMolde.Descricao)
            ? "Sem descricao disponivel."
            : SelectedMolde.Descricao;
    public string ContextosHint => HasMultipleContextos
        ? "Este molde existe em mais do que uma encomenda. Escolhe o contexto certo antes de pre-visualizar ou gerar."
        : "O contexto da encomenda sera usado para localizar a FLT e as fichas editaveis relacionadas.";

    partial void OnSearchTermChanged(string value)
    {
        AplicarFiltro();
        OnPropertyChanged(nameof(HasSearch));
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
        if (_catalogoCarregado)
            return;

        await CarregarCatalogoAsync();
    }

    [RelayCommand]
    private async Task RecarregarAsync()
    {
        await CarregarCatalogoAsync(forceRefresh: true);
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
            PreviewTitulo = $"Pre-visualizacao {request.TipoRelatorio}";
            PreviewResumo = $"O contexto do relatorio foi validado para o molde {SelectedMolde.Numero}. Se estiver tudo certo, podes gerar o ficheiro definitivo a seguir.";
            PreviewOrigem = "Pre-visualizacao validada sem gerar ficheiro.";
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
        IsGenerating = true;

        try
        {
            var request = await ResolverPedidoAsync();
            if (request is null)
                return;

            var generated = await _relatoriosService.GenerateAsync(request);

            HasPreview = true;
            IsReportAvailable = true;
            PreviewTitulo = $"Relatorio {request.TipoRelatorio} pronto";
            PreviewResumo = $"O relatorio foi gerado com sucesso para o molde {SelectedMolde.Numero}.";
            PreviewOrigem = $"Ficheiro gerado: {generated.FileName}";

            await OpenFileAsync(generated, $"Relatorio {request.TipoRelatorio}");
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

    private async Task CarregarCatalogoAsync(bool forceRefresh = false)
    {
        if (IsLoadingCatalogo)
            return;

        IsLoadingCatalogo = true;
        ErrorMessage = string.Empty;

        try
        {
            if (forceRefresh)
            {
                _catalogoCompleto.Clear();
                Moldes.Clear();
                ContextosEncomenda.Clear();
                OnPropertyChanged(nameof(HasContextos));
                OnPropertyChanged(nameof(HasMultipleContextos));
                OnPropertyChanged(nameof(ContextosHint));
                SelectedMolde = null;
                SelectedContexto = null;
                _fichasDoMoldeSelecionado.Clear();
                _fichasMoldeIdEmCache = null;
                _catalogoCarregado = false;
                ResetPreviewState();
            }

            if (_catalogoCarregado)
                return;

            var paginaAtual = 1;
            var totalPaginas = 1;
            var moldes = new List<MoldeDto>();

            do
            {
                var pagina = await _moldesService.GetAllAsync(paginaAtual, MoldesPageSize);
                if (pagina is null)
                {
                    ErrorMessage = "Nao foi possivel carregar a lista de moldes.";
                    return;
                }

                moldes.AddRange(pagina.Items);
                totalPaginas = Math.Max(1, pagina.TotalPages);
                paginaAtual++;
            }
            while (paginaAtual <= totalPaginas);

            _catalogoCompleto.Clear();
            _catalogoCompleto.AddRange(moldes
                .OrderBy(item => item.Numero)
                .ThenBy(item => item.Nome));

            _catalogoCarregado = true;
            AplicarFiltro();
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

            SelectedContexto = ContextosEncomenda.FirstOrDefault();

            if (SelectedContexto is null)
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

    private void AplicarFiltro()
    {
        var termo = SearchTerm.Trim();
        IEnumerable<MoldeDto> resultado = _catalogoCompleto;

        if (!string.IsNullOrWhiteSpace(termo))
        {
            resultado = resultado.Where(item =>
                item.Numero.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                item.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                item.NumeroMoldeCliente.Contains(termo, StringComparison.OrdinalIgnoreCase));
        }

        Moldes.Clear();
        foreach (var molde in resultado)
            Moldes.Add(molde);
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
            PreviewResumo = $"Vai ser usada a associacao Encomenda-Molde {SelectedContexto.EncomendaMolde_id} para gerar a FLT.";
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

        var fichas = await ObterFichasDoMoldeSelecionadoAsync();
        var ficha = fichas
            .Where(item => item.EncomendaMoldeId == SelectedContexto.EncomendaMolde_id &&
                           string.Equals(item.TipoDisplay, tipo, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.DataCriacao)
            .FirstOrDefault();

        if (ficha is null)
        {
            IsReportAvailable = false;
            HasPreview = true;
            PreviewTitulo = $"Sem ficha {tipo}";
            PreviewResumo = $"Nao existe nenhuma ficha {tipo} associada ao molde {SelectedMolde.Numero} no contexto comercial selecionado.";
            PreviewDetalhes = BuildPreviewDetails(null);
            PreviewOrigem = $"Contexto comercial: {SelectedContexto.ContextoDisplay}";

            await _dialogService.ShowInfoAsync(
                "Relatorio indisponivel",
                $"Nao existe nenhuma ficha {tipo} para o molde {SelectedMolde.Numero} neste contexto.");

            return null;
        }

        IsReportAvailable = true;
        PreviewTitulo = $"{tipo} pronta a exportar";
        PreviewResumo = $"Foi localizada a ficha {tipo} mais recente para este contexto.";
        PreviewDetalhes = BuildPreviewDetails(ficha);
        PreviewOrigem = $"Ficha encontrada: #{ficha.FichaProducaoId} em {ficha.DataCriacao:dd/MM/yyyy HH:mm}";

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

    private string BuildPreviewDetails(FichaProducaoResumoDto? ficha)
    {
        if (SelectedMolde is null || SelectedContexto is null)
            return string.Empty;

        var linhas = new List<string>
        {
            $"Molde: {SelectedMolde.Numero} - {SelectedMolde.Nome}",
            $"Tipo: {SelectedTipoRelatorio}",
            $"Encomenda-Molde: #{SelectedContexto.EncomendaMolde_id}",
            $"Encomenda cliente: {SelectedContexto.NumeroEncomendaClienteDisplay}",
            $"Entrega prevista: {SelectedContexto.DataEntregaPrevistaDisplay}",
            $"Prioridade: {SelectedContexto.Prioridade}"
        };

        if (ficha is not null)
            linhas.Add($"Ficha usada: #{ficha.FichaProducaoId} ({ficha.DataCriacao:dd/MM/yyyy HH:mm})");

        return string.Join(Environment.NewLine, linhas);
    }

    private async Task OpenFileAsync(RelatorioFileResult file, string title)
    {
        await Launcher.Default.OpenAsync(new OpenFileRequest(
            title,
            new ReadOnlyFile(file.FilePath)));
    }

    private void ResetPreviewState()
    {
        HasPreview = false;
        IsReportAvailable = false;
        PreviewTitulo = "Sem pre-visualizacao";
        PreviewResumo = "Seleciona um molde, escolhe o tipo de relatorio e usa a pre-visualizacao para validar o contexto antes de gerar.";
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
