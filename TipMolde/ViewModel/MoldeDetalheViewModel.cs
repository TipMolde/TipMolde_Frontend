using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class MoldeDetalheViewModel : ObservableObject
{
    private const string ValorNaoDefinido = "Nao definido";

    private readonly MoldesService _moldesService;
    private readonly MoldePdfService _moldePdfService;
    private readonly SessaoPersistidaService _sessaoPersistidaService;
    private readonly UtilizadoresService _utilizadoresService;
    private readonly IDialogService _dialogService;
    private bool _roleLoaded;

    public MoldeDetalheViewModel(
        MoldesService moldesService,
        MoldePdfService moldePdfService,
        SessaoPersistidaService sessaoPersistidaService,
        UtilizadoresService utilizadoresService,
        IDialogService dialogService)
    {
        _moldesService = moldesService;
        _moldePdfService = moldePdfService;
        _sessaoPersistidaService = sessaoPersistidaService;
        _utilizadoresService = utilizadoresService;
        _dialogService = dialogService;
    }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

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

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasDashboard => Dashboard is not null;
    public bool CanGeneratePdf => IsAdmin && HasDashboard && !IsGeneratingPdf;
    public string NumeroDisplay => string.IsNullOrWhiteSpace(Numero) ? ValorNaoDefinido : Numero;
    public string NumeroMoldeClienteDisplay => string.IsNullOrWhiteSpace(NumeroMoldeCliente) ? ValorNaoDefinido : NumeroMoldeCliente;
    public string NomeDisplay => string.IsNullOrWhiteSpace(Nome) ? ValorNaoDefinido : Nome;
    public string DescricaoDisplay => string.IsNullOrWhiteSpace(Descricao) ? ValorNaoDefinido : Descricao;
    public string TipoPedidoDisplay => string.IsNullOrWhiteSpace(TipoPedido) ? ValorNaoDefinido : TipoPedido;
    public string PercentagemConclusaoDisplay => Dashboard is null ? ValorNaoDefinido : $"{Dashboard.PercentagemConclusao:0.##}%";
    public int DistribuicaoTotal => Dashboard is null ? 0 : Dashboard.Maquinacao + Dashboard.Erosao + Dashboard.Montagem + Dashboard.MaterialPendente;
    public string PdfButtonText => IsGeneratingPdf ? "A gerar PDF..." : "Gerar PDF";
    public string MaquinacaoDistribuicaoDisplay => BuildDistribuicaoDisplay(Dashboard?.Maquinacao ?? 0);
    public string ErosaoDistribuicaoDisplay => BuildDistribuicaoDisplay(Dashboard?.Erosao ?? 0);
    public string MontagemDistribuicaoDisplay => BuildDistribuicaoDisplay(Dashboard?.Montagem ?? 0);
    public string MaterialPendenteDistribuicaoDisplay => BuildDistribuicaoDisplay(Dashboard?.MaterialPendente ?? 0);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
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

    public async Task LoadAsync(int moldeId)
    {
        MoldeId = moldeId;
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
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
                return;
            }

            Numero = molde.Numero ?? string.Empty;
            NumeroMoldeCliente = molde.NumeroMoldeCliente ?? string.Empty;
            Nome = molde.Nome ?? string.Empty;
            Descricao = molde.Descricao ?? string.Empty;
            Numero_cavidades = molde.Numero_cavidades;
            TipoPedido = molde.TipoPedido ?? string.Empty;
            Dashboard = dashboardTask.Result;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Dashboard = null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGeneratePdf))]
    private async Task GerarPdfAsync()
    {
        if (Dashboard is null)
            return;

        IsGeneratingPdf = true;

        try
        {
            var filePath = await _moldePdfService.GenerateCicloVidaPdfAsync(
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

    private async Task EnsureUserRoleAsync()
    {
        if (_roleLoaded)
            return;

        _roleLoaded = true;

        var currentUserId = _sessaoPersistidaService.TryGetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            IsAdmin = false;
            return;
        }

        try
        {
            var utilizador = await _utilizadoresService.GetUtilizadorByIdAsync(currentUserId.Value);
            IsAdmin = string.Equals(utilizador.Role, "Admin", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            IsAdmin = false;
        }
    }

    private string BuildDistribuicaoDisplay(int value)
    {
        if (DistribuicaoTotal <= 0)
            return $"{value} pecas (0%)";

        var percentagem = (decimal)value / DistribuicaoTotal * 100m;
        return $"{value} pecas ({percentagem:0.#}%)";
    }
}
