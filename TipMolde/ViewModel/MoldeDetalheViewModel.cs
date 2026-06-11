using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

public partial class MoldeDetalheViewModel : PaginatedViewModel
{
    private const string ValorNaoDefinido = "Nao definido";

    private readonly MoldesService _moldesService;
    private readonly PecasService _pecasService;
    private readonly AuthorizationService _authorizationService;
    private readonly IDialogService _dialogService;
    private bool _roleLoaded;

    public MoldeDetalheViewModel(
        MoldesService moldesService,
        PecasService pecasService,
        AuthorizationService authorizationService,
        IDialogService dialogService)
    {
        _moldesService = moldesService;
        _pecasService = pecasService;
        _authorizationService = authorizationService;
        _dialogService = dialogService;
        PageSize = 8;
    }

    [ObservableProperty]
    private int moldeId;

    [ObservableProperty]
    private string numero = string.Empty;

    [ObservableProperty]
    private string numeroMoldeCliente = string.Empty;

    [ObservableProperty]
    private string nome = string.Empty;

    [ObservableProperty]
    private string descricao = string.Empty;

    [ObservableProperty]
    private int numero_cavidades;

    [ObservableProperty]
    private string tipoPedido = string.Empty;

    [ObservableProperty]
    private MoldeCicloVidaDashboardDto? dashboard;

    [ObservableProperty]
    private bool isAdmin;

    [ObservableProperty]
    private bool isGeneratingPdf;

    [ObservableProperty]
    private bool canManagePieces;

    public bool HasDashboard => Dashboard is not null;
    public bool CanGeneratePdf => IsAdmin && HasDashboard && !IsGeneratingPdf;
    public bool HasPecas => Pecas.Count > 0;
    public string NumeroDisplay => string.IsNullOrWhiteSpace(Numero) ? ValorNaoDefinido : Numero;
    public string NumeroMoldeClienteDisplay => string.IsNullOrWhiteSpace(NumeroMoldeCliente) ? ValorNaoDefinido : NumeroMoldeCliente;
    public string NomeDisplay => string.IsNullOrWhiteSpace(Nome) ? ValorNaoDefinido : Nome;
    public string DescricaoDisplay => string.IsNullOrWhiteSpace(Descricao) ? ValorNaoDefinido : Descricao;
    public string TipoPedidoDisplay => string.IsNullOrWhiteSpace(TipoPedido) ? ValorNaoDefinido : TipoPedido;
    public string PercentagemConclusaoDisplay => Dashboard is null ? ValorNaoDefinido : $"{Dashboard.PercentagemConclusao:0.##}%";
    public int DistribuicaoTotal => Dashboard is null ? 0 : Dashboard.Maquinacao + Dashboard.Erosao + Dashboard.Montagem + Dashboard.MaterialPendente + Dashboard.EmEspera;
    public string PdfButtonText => IsGeneratingPdf ? "A gerar PDF..." : "Gerar PDF";
    public string MaquinacaoDistribuicaoDisplay => BuildDistribuicaoDisplay(Dashboard?.Maquinacao ?? 0);
    public string ErosaoDistribuicaoDisplay => BuildDistribuicaoDisplay(Dashboard?.Erosao ?? 0);
    public string MontagemDistribuicaoDisplay => BuildDistribuicaoDisplay(Dashboard?.Montagem ?? 0);
    public string MaterialPendenteDistribuicaoDisplay => BuildDistribuicaoDisplay(Dashboard?.MaterialPendente ?? 0);
    public string EmEsperaDistribuicaoDisplay => BuildDistribuicaoDisplay(Dashboard?.EmEspera ?? 0);
    public string EmptyPecasMessage => "Este molde ainda nao tem pecas registadas.";
    public string PecasSectionDescription => CanManagePieces
        ? "Edita prioridades, quantidades e remove pecas erradas diretamente a partir deste detalhe."
        : "Consulta as pecas registadas para este molde.";
    public ObservableCollection<PecaDto> Pecas { get; } = new();

    partial void OnNumeroChanged(string value) => OnPropertyChanged(nameof(NumeroDisplay));
    partial void OnNumeroMoldeClienteChanged(string value) => OnPropertyChanged(nameof(NumeroMoldeClienteDisplay));
    partial void OnNomeChanged(string value) => OnPropertyChanged(nameof(NomeDisplay));
    partial void OnDescricaoChanged(string value) => OnPropertyChanged(nameof(DescricaoDisplay));
    partial void OnTipoPedidoChanged(string value) => OnPropertyChanged(nameof(TipoPedidoDisplay));
    partial void OnDashboardChanged(MoldeCicloVidaDashboardDto? value)
    {
        OnPropertyChanged(nameof(HasDashboard));
        OnPropertyChanged(nameof(PercentagemConclusaoDisplay));
        OnPropertyChanged(nameof(DistribuicaoTotal));
        OnPropertyChanged(nameof(MaquinacaoDistribuicaoDisplay));
        OnPropertyChanged(nameof(ErosaoDistribuicaoDisplay));
        OnPropertyChanged(nameof(MontagemDistribuicaoDisplay));
        OnPropertyChanged(nameof(MaterialPendenteDistribuicaoDisplay));
        OnPropertyChanged(nameof(EmEsperaDistribuicaoDisplay));
        OnPropertyChanged(nameof(CanGeneratePdf));
        GerarPdfCommand.NotifyCanExecuteChanged();
    }
    partial void OnIsAdminChanged(bool value)
    {
        OnPropertyChanged(nameof(CanGeneratePdf));
        GerarPdfCommand.NotifyCanExecuteChanged();
    }
    partial void OnIsGeneratingPdfChanged(bool value)
    {
        OnPropertyChanged(nameof(CanGeneratePdf));
        OnPropertyChanged(nameof(PdfButtonText));
        GerarPdfCommand.NotifyCanExecuteChanged();
    }
    partial void OnCanManagePiecesChanged(bool value) => OnPropertyChanged(nameof(PecasSectionDescription));

    public async Task LoadAsync(int moldeId)
    {
        MoldeId = moldeId;
        ErrorMessage = string.Empty;

        try
        {
            await ExecutePagedLoadAsync(async () =>
            {
                await EnsureUserRoleAsync();

                var moldeTask = _moldesService.GetByIdAsync(moldeId);
                var dashboardTask = _moldesService.GetDashboardCicloVidaAsync(moldeId);

                await Task.WhenAll(moldeTask, dashboardTask);

                var molde = moldeTask.Result;
                if (molde is null)
                {
                    ErrorMessage = "Nao foi possivel carregar o detalhe do molde.";
                    Dashboard = null;
                    Pecas.Clear();
                    OnPropertyChanged(nameof(HasPecas));
                    return;
                }

                Numero = molde.Numero ?? string.Empty;
                NumeroMoldeCliente = molde.NumeroMoldeCliente ?? string.Empty;
                Nome = molde.Nome ?? string.Empty;
                Descricao = molde.Descricao ?? string.Empty;
                Numero_cavidades = molde.Numero_cavidades;
                TipoPedido = molde.TipoPedido ?? string.Empty;
                Dashboard = dashboardTask.Result;
                Page = 1;
                await LoadPecasAsync();
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Dashboard = null;
            Pecas.Clear();
            OnPropertyChanged(nameof(HasPecas));
        }
    }

    public async Task RefreshPecasAsync()
    {
        if (MoldeId <= 0)
            return;

        ErrorMessage = string.Empty;

        try
        {
            await ExecutePagedLoadAsync(LoadPecasAsync);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    protected override Task LoadPageAsync()
    {
        if (MoldeId <= 0)
            return Task.CompletedTask;

        return ExecutePagedLoadAsync(LoadPecasAsync);
    }

    [RelayCommand(CanExecute = nameof(CanGeneratePdf))]
    private async Task GerarPdfAsync()
    {
        if (Dashboard is null)
            return;

        IsGeneratingPdf = true;

        try
        {
            var filePath = await MoldePdfService.GenerateCicloVidaPdfAsync(
                NumeroDisplay,
                NomeDisplay,
                DescricaoDisplay,
                TipoPedidoDisplay,
                Numero_cavidades,
                Dashboard);

            await Launcher.Default.OpenAsync(new OpenFileRequest(
                "Relatorio do ciclo de vida",
                new ReadOnlyFile(filePath)));

            await _dialogService.ShowSuccessAsync(
                "PDF gerado",
                $"O PDF do ciclo de vida foi gerado em {filePath}.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Erro", ex.Message);
        }
        finally
        {
            IsGeneratingPdf = false;
        }
    }

    [RelayCommand]
    private async Task VoltarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task AdicionarPecaAsync()
    {
        if (!CanManagePieces)
            return;

        if (MoldeId <= 0)
            return;

        var numeroMolde = Uri.EscapeDataString(NumeroDisplay);
        await Shell.Current.GoToAsync($"{nameof(AdicionarPecaPage)}?molde_id={MoldeId}&numero_molde={numeroMolde}");
    }

    [RelayCommand]
    private async Task EditarPecaAsync(PecaDto? peca)
    {
        if (!CanManagePieces)
            return;

        if (peca is null || peca.PecaId <= 0)
            return;

        var numeroMolde = Uri.EscapeDataString(NumeroDisplay);
        await Shell.Current.GoToAsync($"{nameof(EditarPecaPage)}?peca_id={peca.PecaId}&numero_molde={numeroMolde}");
    }

    [RelayCommand]
    private async Task ApagarPecaAsync(PecaDto? peca)
    {
        if (!CanManagePieces)
            return;

        if (peca is null || peca.PecaId <= 0)
            return;

        var nomePeca = string.IsNullOrWhiteSpace(peca.Designacao) ? $"a peca {peca.PecaId}" : $"a peca {peca.Designacao}";
        var confirmado = await _dialogService.ConfirmDeleteAsync(nomePeca);
        if (!confirmado)
            return;

        try
        {
            await _pecasService.DeleteAsync(peca.PecaId);

            if (Pecas.Count == 1 && Page > 1)
                Page--;

            await RefreshPecasAsync();

            await _dialogService.ShowSuccessAsync(
                "Peca eliminada",
                $"A peca {peca.Designacao} foi eliminada com sucesso.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Erro", ex.Message);
        }
    }

    private async Task EnsureUserRoleAsync()
    {
        if (_roleLoaded)
            return;

        _roleLoaded = true;

        try
        {
            await _authorizationService.GetCurrentRoleAsync();
            IsAdmin = _authorizationService.CanCreateMachines();
            CanManagePieces = _authorizationService.CanManagePieces();
        }
        catch
        {
            IsAdmin = false;
            CanManagePieces = false;
        }
    }

    private string BuildDistribuicaoDisplay(int value)
    {
        if (DistribuicaoTotal <= 0)
            return $"{value} pecas (0%)";

        var percentagem = (decimal)value / DistribuicaoTotal * 100m;
        return $"{value} pecas ({percentagem:0.#}%)";
    }

    private async Task LoadPecasAsync()
    {
        var pagina = await _pecasService.GetByMoldeIdAsync(MoldeId, Page, PageSize);
        if (pagina is null)
        {
            Pecas.Clear();
            UpdatePagination(0, 1);
            OnPropertyChanged(nameof(HasPecas));
            return;
        }

        UpdatePagination(pagina.TotalItems, pagina.TotalPages);

        Pecas.Clear();
        foreach (var peca in pagina.Items.OrderBy(item => item.Prioridade).ThenBy(item => item.NumeroPeca).ThenBy(item => item.Designacao))
            Pecas.Add(peca);

        OnPropertyChanged(nameof(HasPecas));
        OnPropertyChanged(nameof(EmptyPecasMessage));
    }
}
