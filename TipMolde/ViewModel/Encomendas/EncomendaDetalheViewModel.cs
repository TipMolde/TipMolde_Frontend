using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;

namespace TipMolde.ViewModel;

/// <summary>
/// Apresenta o detalhe da encomenda, permite ajustar datas de entrega e gerir o estado operacional dos moldes associados.
/// </summary>
public partial class EncomendaDetalheViewModel : ObservableObject
{
    private const string ValorNaoDefinido = "Nao definido";

    private readonly EncomendasService _encomendasService;
    private readonly MoldesService _moldesService;
    private readonly ClientesService _clientesService;
    private readonly GlobalMoldePriorityService _globalMoldePriorityService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Construtor do view model de detalhe da encomenda.
    /// </summary>
    public EncomendaDetalheViewModel(
        EncomendasService encomendasService,
        MoldesService moldesService,
        ClientesService clientesService,
        GlobalMoldePriorityService globalMoldePriorityService,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        _encomendasService = encomendasService;
        _moldesService = moldesService;
        _clientesService = clientesService;
        _globalMoldePriorityService = globalMoldePriorityService;
        _dialogService = dialogService;
        _navigationService = navigationService;
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
    public bool CanGerirMoldes => Encomenda_id > 0 &&
                                  !IsLoading &&
                                  !IsCancelling &&
                                  !string.Equals(Estado, "CONCLUIDA", StringComparison.OrdinalIgnoreCase) &&
                                  !string.Equals(Estado, "CANCELADA", StringComparison.OrdinalIgnoreCase);

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCancelEncomenda));
        OnPropertyChanged(nameof(CanGerirMoldes));
        UpdateMoldeAvailability();
    }

    partial void OnIsCancellingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCancelEncomenda));
        OnPropertyChanged(nameof(CanGerirMoldes));
        UpdateMoldeAvailability();
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
        OnPropertyChanged(nameof(CanGerirMoldes));
        UpdateMoldeAvailability();
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
            var associacoesPagina = await associacoesTask;
            var moldesPagina = await moldesTask;

            var associacoes = associacoesPagina?.Items ?? [];
            var moldes = moldesPagina?.Items ?? [];
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
                    Estado = associacao.Estado
                });
            }

            QuantidadeTotalPrevista = associacoes.Sum(item => item.Quantidade);
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

        await _navigationService.GoToAsync($"{nameof(MoldeDetalhePage)}?molde_id={molde.MoldeId}");
    }

    [RelayCommand]
    private async Task VoltarAsync()
    {
        await _navigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task CancelarEncomendaAsync()
    {
        if (!CanCancelEncomenda)
            return;

        var confirmar = await _dialogService.ConfirmAsync(
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
    private async Task GuardarPrazoMoldeAsync(EncomendaMoldeItemDto? molde)
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
    private Task IniciarProducaoMoldeAsync(EncomendaMoldeItemDto? molde)
    {
        return AtualizarEstadoMoldeAsync(
            molde,
            "EM_PRODUCAO",
            "Inicio de producao",
            item => $"O molde {item.NumeroMoldeDisplay} foi colocado em producao.");
    }

    [RelayCommand]
    private Task ConcluirMoldeAsync(EncomendaMoldeItemDto? molde)
    {
        return AtualizarEstadoMoldeAsync(
            molde,
            "CONCLUIDO",
            "Conclusao do molde",
            item => $"O molde {item.NumeroMoldeDisplay} foi marcado como concluido.");
    }

    private async Task AtualizarEstadoMoldeAsync(
        EncomendaMoldeItemDto? molde,
        string estadoDestino,
        string successTitle,
        Func<EncomendaMoldeItemDto, string> successMessageFactory)
    {
        if (molde is null || !CanGerirMoldes || molde.EncomendaMoldeId <= 0)
            return;

        ErrorMessage = string.Empty;

        try
        {
            await _encomendasService.UpdateEncomendaMoldeEstadoAsync(molde.EncomendaMoldeId, estadoDestino);
            await LoadAsync(Encomenda_id);

            await _dialogService.ShowSuccessAsync(
                successTitle,
                successMessageFactory(molde));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void NotifyMoldeStateChanged()
    {
        OnPropertyChanged(nameof(HasMoldes));
        OnPropertyChanged(nameof(HasNoMoldes));
        OnPropertyChanged(nameof(TotalMoldesAssociados));
        UpdateMoldeAvailability();
    }

    private void UpdateMoldeAvailability()
    {
        foreach (var molde in Moldes)
            molde.CanGerirMolde = CanGerirMoldes;
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
