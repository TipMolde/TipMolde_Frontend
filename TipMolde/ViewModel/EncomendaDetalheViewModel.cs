using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;

namespace TipMolde.ViewModel;

/// <summary>
/// Apresenta o detalhe da encomenda, permite ajustar datas de entrega e registar entregas parciais por molde.
/// </summary>
public partial class EncomendaDetalheViewModel : ObservableObject
{
    private const string ValorNaoDefinido = "Nao definido";

    private readonly EncomendasService _encomendasService;
    private readonly MoldesService _moldesService;
    private readonly ClientesService _clientesService;
    private readonly GlobalMoldePriorityService _globalMoldePriorityService;
    private readonly IDialogService _dialogService;

    /// <summary>
    /// Construtor do view model de detalhe da encomenda.
    /// </summary>
    public EncomendaDetalheViewModel(
        EncomendasService encomendasService,
        MoldesService moldesService,
        ClientesService clientesService,
        GlobalMoldePriorityService globalMoldePriorityService,
        IDialogService dialogService)
    {
        _encomendasService = encomendasService;
        _moldesService = moldesService;
        _clientesService = clientesService;
        _globalMoldePriorityService = globalMoldePriorityService;
        _dialogService = dialogService;
    }

    public ObservableCollection<EncomendaMoldeItemDto> Moldes { get; } = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isCancelling;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private int encomenda_id;

    [ObservableProperty]
    private string numeroEncomendaCliente = string.Empty;

    [ObservableProperty]
    private string nomeCliente = string.Empty;

    [ObservableProperty]
    private string nomeServicoCliente = string.Empty;

    [ObservableProperty]
    private string nomeResponsavelCliente = string.Empty;

    [ObservableProperty]
    private string numeroProjetoCliente = string.Empty;

    [ObservableProperty]
    private string estado = string.Empty;

    [ObservableProperty]
    private DateTime dataRegisto;

    [ObservableProperty]
    private int quantidadeTotalPrevista;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasMoldes => Moldes.Count > 0;
    public bool HasNoMoldes => Moldes.Count == 0;
    public string NumeroEncomendaClienteDisplay => string.IsNullOrWhiteSpace(NumeroEncomendaCliente) ? ValorNaoDefinido : NumeroEncomendaCliente;
    public string NomeClienteDisplay => string.IsNullOrWhiteSpace(NomeCliente) ? ValorNaoDefinido : NomeCliente;
    public string NomeServicoClienteDisplay => string.IsNullOrWhiteSpace(NomeServicoCliente) ? ValorNaoDefinido : NomeServicoCliente;
    public string NomeResponsavelClienteDisplay => string.IsNullOrWhiteSpace(NomeResponsavelCliente) ? ValorNaoDefinido : NomeResponsavelCliente;
    public string NumeroProjetoClienteDisplay => string.IsNullOrWhiteSpace(NumeroProjetoCliente) ? ValorNaoDefinido : NumeroProjetoCliente;
    public string EstadoDisplay => string.IsNullOrWhiteSpace(Estado) ? ValorNaoDefinido : Estado.Replace('_', ' ');
    public int TotalMoldesAssociados => Moldes.Count;
    public string EmptyMoldesMessage => "Esta encomenda ainda nao tem moldes associados.";
    public bool CanCancelEncomenda => Encomenda_id > 0 &&
                                      !IsLoading &&
                                      !IsCancelling &&
                                      (string.Equals(Estado, "CONFIRMADA", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(Estado, "EM_PRODUCAO", StringComparison.OrdinalIgnoreCase));
    public bool CanEditarEntregaMoldes => Encomenda_id > 0 &&
                                          !IsLoading &&
                                          !IsCancelling &&
                                          !string.Equals(Estado, "CONCLUIDA", StringComparison.OrdinalIgnoreCase) &&
                                          !string.Equals(Estado, "CANCELADA", StringComparison.OrdinalIgnoreCase);

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCancelEncomenda));
        OnPropertyChanged(nameof(CanEditarEntregaMoldes));
        UpdateEntregaAvailability();
    }

    partial void OnIsCancellingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCancelEncomenda));
        OnPropertyChanged(nameof(CanEditarEntregaMoldes));
        UpdateEntregaAvailability();
    }
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnNomeClienteChanged(string value) => OnPropertyChanged(nameof(NomeClienteDisplay));
    partial void OnNumeroEncomendaClienteChanged(string value) => OnPropertyChanged(nameof(NumeroEncomendaClienteDisplay));
    partial void OnNomeServicoClienteChanged(string value) => OnPropertyChanged(nameof(NomeServicoClienteDisplay));
    partial void OnNomeResponsavelClienteChanged(string value) => OnPropertyChanged(nameof(NomeResponsavelClienteDisplay));
    partial void OnNumeroProjetoClienteChanged(string value) => OnPropertyChanged(nameof(NumeroProjetoClienteDisplay));
    partial void OnEstadoChanged(string value)
    {
        OnPropertyChanged(nameof(EstadoDisplay));
        OnPropertyChanged(nameof(CanCancelEncomenda));
        OnPropertyChanged(nameof(CanEditarEntregaMoldes));
        UpdateEntregaAvailability();
    }
    partial void OnQuantidadeTotalPrevistaChanged(int value) => OnPropertyChanged(nameof(QuantidadeTotalPrevista));

    /// <summary>
    /// Carrega a encomenda, os seus moldes associados e o resumo apresentado no detalhe.
    /// </summary>
    public async Task LoadAsync(int encomendaId)
    {
        Encomenda_id = encomendaId;
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            var encomenda = await _encomendasService.GetByIdAsync(encomendaId);
            if (encomenda is null)
            {
                ErrorMessage = "Nao foi possivel carregar o detalhe da encomenda.";
                Moldes.Clear();
                NotifyMoldeStateChanged();
                return;
            }

            NumeroEncomendaCliente = encomenda.NumeroEncomendaCliente ?? string.Empty;
            NomeCliente = encomenda.NomeCliente ?? string.Empty;
            NomeServicoCliente = encomenda.NomeServicoCliente ?? string.Empty;
            NomeResponsavelCliente = encomenda.NomeResponsavelCliente ?? string.Empty;
            NumeroProjetoCliente = encomenda.NumeroProjetoCliente ?? string.Empty;
            Estado = encomenda.Estado ?? string.Empty;
            DataRegisto = encomenda.DataRegisto;

            if (string.IsNullOrWhiteSpace(NomeCliente) && encomenda.Cliente_id > 0)
            {
                var cliente = await _clientesService.GetByIdAsync(encomenda.Cliente_id);
                NomeCliente = cliente?.Nome?.Trim() ?? string.Empty;
            }

            var associacoesTask = _encomendasService.GetEncomendaMoldesByEncomendaIdAsync(encomendaId, 1, 100);
            var moldesTask = _moldesService.GetByEncomendaIdAsync(encomendaId, 1, 100);

            await Task.WhenAll(associacoesTask, moldesTask);

            var associacoes = associacoesTask.Result?.Items ?? [];
            var moldes = moldesTask.Result?.Items ?? [];
            var moldesPorId = moldes.ToDictionary(molde => molde.MoldeId);

            Moldes.Clear();

            foreach (var associacao in associacoes.OrderBy(item => item.Prioridade).ThenBy(item => item.Molde_id))
            {
                moldesPorId.TryGetValue(associacao.Molde_id, out var molde);

                Moldes.Add(new EncomendaMoldeItemDto
                {
                    EncomendaMoldeId = associacao.EncomendaMolde_id,
                    MoldeId = associacao.Molde_id,
                    NumeroMolde = FirstNonEmpty(associacao.NumeroMolde, molde?.Numero),
                    NomeMolde = molde?.Nome ?? string.Empty,
                    DescricaoMolde = molde?.Descricao ?? string.Empty,
                    ImagemCapaPath = molde?.ImagemCapaPath ?? string.Empty,
                    NumeroCavidades = molde?.Numero_cavidades ?? 0,
                    Quantidade = associacao.Quantidade,
                    Prioridade = associacao.Prioridade,
                    DataEntregaPrevista = associacao.DataEntregaPrevista,
                    QuantidadePorEntregar = Math.Max(0, associacao.QuantidadePorEntregar ?? associacao.Quantidade),
                    IsEntregue = associacao.Entregue ?? ((associacao.QuantidadePorEntregar ?? associacao.Quantidade) <= 0)
                });
            }

            QuantidadeTotalPrevista = associacoes.Sum(item => item.Quantidade);
            ApplyDeliveredStateFromLoadedData();
            NotifyMoldeStateChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Moldes.Clear();
            NotifyMoldeStateChanged();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AbrirMoldeAsync(EncomendaMoldeItemDto? molde)
    {
        if (molde is null || molde.MoldeId <= 0)
            return;

        await Shell.Current.GoToAsync($"{nameof(MoldeDetalhePage)}?molde_id={molde.MoldeId}");
    }

    [RelayCommand]
    private async Task VoltarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelarEncomendaAsync()
    {
        if (!CanCancelEncomenda)
            return;

        var confirmar = await _dialogService.GetCurrentPage().DisplayAlert(
            "Cancelar encomenda",
            $"Pretende cancelar a encomenda {NumeroEncomendaClienteDisplay}?",
            "Cancelar encomenda",
            "Voltar");

        if (!confirmar)
            return;

        IsCancelling = true;
        ErrorMessage = string.Empty;

        try
        {
            await _encomendasService.UpdateEstadoAsync(Encomenda_id, "CANCELADA");
            Estado = "CANCELADA";

            // Depois de sair da fila por cancelamento, os restantes moldes precisam de nova prioridade.
            try
            {
                await _globalMoldePriorityService.RebalanceAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"A encomenda {NumeroEncomendaClienteDisplay} foi cancelada, mas nao foi possivel recalcular as prioridades globais. Detalhe: {ex.Message}");
            }

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                $"A encomenda {NumeroEncomendaClienteDisplay} foi cancelada com sucesso.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsCancelling = false;
        }
    }

    [RelayCommand]
    private async Task GuardarEntregaMoldeAsync(EncomendaMoldeItemDto? molde)
    {
        if (molde is null || molde.EncomendaMoldeId <= 0)
            return;

        ErrorMessage = string.Empty;

        try
        {
            await _encomendasService.UpdateEncomendaMoldeAsync(
                molde.EncomendaMoldeId,
                dataEntregaPrevista: molde.DataEntregaPrevista);

            // Recalcula e volta a carregar para refletir a nova ordem global apos a alteracao da data.
            try
            {
                await _globalMoldePriorityService.RebalanceAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"A data de entrega do molde {molde.NumeroMoldeDisplay} foi atualizada, mas nao foi possivel recalcular as prioridades globais. Detalhe: {ex.Message}");
            }

            await LoadAsync(Encomenda_id);

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                $"A entrega prevista do molde {molde.NumeroMoldeDisplay} foi atualizada.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private Task RegistarEntregaMoldeAsync(EncomendaMoldeItemDto? molde)
    {
        if (molde is null || !CanEditarEntregaMoldes || !molde.CanRegistarEntrega)
            return Task.CompletedTask;

        molde.QuantidadePorEntregar = Math.Max(0, molde.QuantidadePorEntregar - 1);
        RecalculateEstadoFromEntregas();
        UpdateEntregaAvailability();

        return Task.CompletedTask;
    }

    private void NotifyMoldeStateChanged()
    {
        OnPropertyChanged(nameof(HasMoldes));
        OnPropertyChanged(nameof(HasNoMoldes));
        OnPropertyChanged(nameof(TotalMoldesAssociados));
        UpdateEntregaAvailability();
    }

    private void ApplyDeliveredStateFromLoadedData()
    {
        if (Moldes.Count == 0)
            return;

        if (Moldes.All(item => item.QuantidadePorEntregar <= 0))
        {
            Estado = "CONCLUIDA";
            return;
        }

        if (Moldes.Any(item => item.QuantidadePorEntregar < item.Quantidade))
            Estado = "PARCIALMENTE_ENTREGUE";
    }

    private void RecalculateEstadoFromEntregas()
    {
        if (Moldes.Count == 0)
            return;

        if (Moldes.All(item => item.QuantidadePorEntregar <= 0))
        {
            Estado = "CONCLUIDA";
            return;
        }

        if (Moldes.Any(item => item.QuantidadePorEntregar < item.Quantidade))
        {
            Estado = "PARCIALMENTE_ENTREGUE";
            return;
        }
    }

    private void UpdateEntregaAvailability()
    {
        foreach (var molde in Moldes)
            molde.CanEditarEntrega = CanEditarEntregaMoldes;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }
}
