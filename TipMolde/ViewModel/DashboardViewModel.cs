using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;

namespace TipMolde.ViewModel;

public partial class DashboardViewModel : ObservableObject
{
    private const string ValorNaoDefinido = "Nao definido";

    private readonly EncomendasService _encomendasService;
    private readonly MoldesService _moldesService;
    private readonly PecasService _pecasService;
    private readonly AuthorizationService _authorizationService;
    private readonly IDialogService _dialogService;

    private bool _suppressSelectedMoldeRececaoChanged;
    private int _rececaoMaterialLoadVersion;

    public DashboardViewModel(
        EncomendasService encomendasService,
        MoldesService moldesService,
        PecasService pecasService,
        AuthorizationService authorizationService,
        IDialogService dialogService)
    {
        _encomendasService = encomendasService;
        _moldesService = moldesService;
        _pecasService = pecasService;
        _authorizationService = authorizationService;
        _dialogService = dialogService;

        PecasPendentesRececao.CollectionChanged += OnPecasPendentesRececaoCollectionChanged;
    }

    public ObservableCollection<MoldeRececaoOption> MoldesRececaoDisponiveis { get; } = new();
    public ObservableCollection<SelectablePecaRececaoItem> PecasPendentesRececao { get; } = new();

    [ObservableProperty]
    private bool isLoadingHero;

    [ObservableProperty]
    private string heroErrorMessage = string.Empty;

    [ObservableProperty]
    private MoldeDto? moldeMaisProximo;

    [ObservableProperty]
    private EncomendaResumoDto? encomendaMaisProxima;

    [ObservableProperty]
    private EncomendaMoldeDto? entregaMoldeMaisProxima;

    [ObservableProperty]
    private MoldeCicloVidaDashboardDto? dashboardMaisProximo;

    [ObservableProperty]
    private int? totalMoldesPorEntregar;

    [ObservableProperty]
    private decimal? taxaConclusao;

    [ObservableProperty]
    private int? encomendasConcluidasUltimosTresMeses;

    [ObservableProperty]
    private int? moldesComAtraso;

    [ObservableProperty]
    private bool canUseRececaoMaterial;

    [ObservableProperty]
    private bool isLoadingRececaoMaterial;

    [ObservableProperty]
    private bool isSavingRececaoMaterial;

    [ObservableProperty]
    private string rececaoMaterialErrorMessage = string.Empty;

    [ObservableProperty]
    private MoldeRececaoOption? selectedMoldeRececao;

    public bool HasHeroError => !string.IsNullOrWhiteSpace(HeroErrorMessage);
    public bool HasMoldeEntregaDashboard =>
        MoldeMaisProximo is not null &&
        EncomendaMaisProxima is not null &&
        EntregaMoldeMaisProxima is not null &&
        DashboardMaisProximo is not null;
    public bool HasNoMoldeEntregaDashboard => !IsLoadingHero && !HasHeroError && !HasMoldeEntregaDashboard;
    public string NomeMoldeMaisProximoDisplay => FirstNonEmpty(MoldeMaisProximo?.Nome, MoldeMaisProximo?.Numero, EntregaMoldeMaisProxima?.NumeroMolde);
    public string NumeroMoldeMaisProximoDisplay => FirstNonEmpty(MoldeMaisProximo?.Numero, EntregaMoldeMaisProxima?.NumeroMolde);
    public string ClienteMoldeMaisProximoDisplay => FirstNonEmpty(EncomendaMaisProxima?.NomeClienteDisplay);
    public string DataEntregaMoldeMaisProximoDisplay => EntregaMoldeMaisProxima is null
        ? ValorNaoDefinido
        : EntregaMoldeMaisProxima.DataEntregaPrevista.ToString("dd/MM/yyyy");
    public string PercentagemConclusaoMaisProximoDisplay => DashboardMaisProximo is null
        ? ValorNaoDefinido
        : $"{DashboardMaisProximo.PercentagemConclusao:0.##}%";
    public string TotalMoldesPorEntregarDisplay => TotalMoldesPorEntregar?.ToString() ?? ValorNaoDefinido;
    public string TaxaConclusaoDisplay => TaxaConclusao.HasValue
        ? $"{TaxaConclusao.Value:0.##}%"
        : ValorNaoDefinido;
    public string EncomendasConcluidasUltimosTresMesesDisplay => EncomendasConcluidasUltimosTresMeses?.ToString() ?? ValorNaoDefinido;
    public string MoldesComAtrasoDisplay => MoldesComAtraso?.ToString() ?? ValorNaoDefinido;
    public string IntervaloUltimosTresMesesDisplay => $"{DateTime.Today.AddMonths(-3):dd/MM/yyyy} - {DateTime.Today:dd/MM/yyyy}";
    public bool HasRececaoMaterialError => !string.IsNullOrWhiteSpace(RececaoMaterialErrorMessage);
    public bool HasMoldesRececaoDisponiveis => MoldesRececaoDisponiveis.Count > 0;
    public bool HasNoMoldesRececaoDisponiveis => CanUseRececaoMaterial && !IsLoadingHero && MoldesRececaoDisponiveis.Count == 0;
    public bool HasSelectedMoldeRececao => SelectedMoldeRececao is not null;
    public bool HasPecasPendentesRececao => PecasPendentesRececao.Count > 0;
    public bool HasNoPecasPendentesRececao => HasSelectedMoldeRececao && !IsLoadingRececaoMaterial && !HasRececaoMaterialError && PecasPendentesRececao.Count == 0;
    public bool CanRegistarChegadaMaterial => CanUseRececaoMaterial &&
                                              SelectedMoldeRececao is not null &&
                                              !IsLoadingRececaoMaterial &&
                                              !IsSavingRececaoMaterial &&
                                              PecasPendentesRececao.Any(item => item.IsSelected);
    public string RececaoMaterialButtonText => IsSavingRececaoMaterial
        ? "A registar chegada..."
        : "Registar chegada de material";
    public string SelectedMoldeRececaoResumo => SelectedMoldeRececao is null
        ? "Seleciona um molde com pedido de material ativo para marcar as pecas que chegaram."
        : $"Encomenda {SelectedMoldeRececao.EncomendaDisplay} | entrega {SelectedMoldeRececao.DataEntregaDisplay}";
    public string PecasRececaoSelectionSummary
    {
        get
        {
            if (PecasPendentesRececao.Count == 0)
                return "Nao existem pecas pendentes para este molde.";

            var selecionadas = PecasPendentesRececao.Count(item => item.IsSelected);
            return selecionadas == 0
                ? $"{PecasPendentesRececao.Count} peca(s) pendente(s). Seleciona as que chegaram."
                : $"{selecionadas} de {PecasPendentesRececao.Count} peca(s) selecionada(s).";
        }
    }

    partial void OnIsLoadingHeroChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoMoldesRececaoDisponiveis));
    }

    partial void OnHeroErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasHeroError));
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
    }

    partial void OnMoldeMaisProximoChanged(MoldeDto? value)
    {
        OnPropertyChanged(nameof(HasMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
        OnPropertyChanged(nameof(NomeMoldeMaisProximoDisplay));
        OnPropertyChanged(nameof(NumeroMoldeMaisProximoDisplay));
        AbrirDashboardMoldeCommand.NotifyCanExecuteChanged();
    }

    partial void OnEncomendaMaisProximaChanged(EncomendaResumoDto? value)
    {
        OnPropertyChanged(nameof(HasMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
        OnPropertyChanged(nameof(ClienteMoldeMaisProximoDisplay));
    }

    partial void OnEntregaMoldeMaisProximaChanged(EncomendaMoldeDto? value)
    {
        OnPropertyChanged(nameof(HasMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
        OnPropertyChanged(nameof(NumeroMoldeMaisProximoDisplay));
        OnPropertyChanged(nameof(DataEntregaMoldeMaisProximoDisplay));
        AbrirDashboardMoldeCommand.NotifyCanExecuteChanged();
    }

    partial void OnDashboardMaisProximoChanged(MoldeCicloVidaDashboardDto? value)
    {
        OnPropertyChanged(nameof(HasMoldeEntregaDashboard));
        OnPropertyChanged(nameof(HasNoMoldeEntregaDashboard));
        OnPropertyChanged(nameof(PercentagemConclusaoMaisProximoDisplay));
    }

    partial void OnTotalMoldesPorEntregarChanged(int? value) => OnPropertyChanged(nameof(TotalMoldesPorEntregarDisplay));
    partial void OnTaxaConclusaoChanged(decimal? value) => OnPropertyChanged(nameof(TaxaConclusaoDisplay));
    partial void OnEncomendasConcluidasUltimosTresMesesChanged(int? value) => OnPropertyChanged(nameof(EncomendasConcluidasUltimosTresMesesDisplay));
    partial void OnMoldesComAtrasoChanged(int? value) => OnPropertyChanged(nameof(MoldesComAtrasoDisplay));

    partial void OnCanUseRececaoMaterialChanged(bool value)
    {
        NotifyRececaoMaterialStateChanged();

        if (!value)
            ResetRececaoMaterial();
    }

    partial void OnIsLoadingRececaoMaterialChanged(bool value) => NotifyRececaoMaterialStateChanged();
    partial void OnIsSavingRececaoMaterialChanged(bool value)
    {
        OnPropertyChanged(nameof(RececaoMaterialButtonText));
        NotifyRececaoMaterialStateChanged();
    }

    partial void OnRececaoMaterialErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasRececaoMaterialError));
        OnPropertyChanged(nameof(HasNoPecasPendentesRececao));
    }

    partial void OnSelectedMoldeRececaoChanged(MoldeRececaoOption? value)
    {
        OnPropertyChanged(nameof(HasSelectedMoldeRececao));
        OnPropertyChanged(nameof(HasNoPecasPendentesRececao));
        OnPropertyChanged(nameof(SelectedMoldeRececaoResumo));
        NotifyRececaoMaterialStateChanged();

        if (_suppressSelectedMoldeRececaoChanged)
            return;

        if (value is null)
        {
            RececaoMaterialErrorMessage = string.Empty;
            ClearPecasPendentesRececao();
            return;
        }

        _ = LoadPecasPendentesRececaoAsync(value.MoldeId);
    }

    public async Task LoadAsync()
    {
        if (IsLoadingHero)
            return;

        var moldeRececaoSelecionadoId = SelectedMoldeRececao?.MoldeId;

        IsLoadingHero = true;
        HeroErrorMessage = string.Empty;

        try
        {
            await RefreshRececaoMaterialAccessAsync();

            var encomendasEmProducaoTask = GetAllEncomendasEmProducaoAsync();
            var todasEncomendasTask = GetTodasEncomendasAsync();
            var filaGlobalMoldesTask = GetAllFilaGlobalMoldeAsync();

            await Task.WhenAll(encomendasEmProducaoTask, todasEncomendasTask, filaGlobalMoldesTask);

            AtualizarResumoExecutivo(
                todasEncomendasTask.Result,
                filaGlobalMoldesTask.Result);

            await CarregarHeroAsync(
                encomendasEmProducaoTask.Result,
                filaGlobalMoldesTask.Result);

            await AtualizarMoldesRececaoAsync(
                filaGlobalMoldesTask.Result,
                moldeRececaoSelecionadoId);
        }
        catch (Exception ex)
        {
            HeroErrorMessage = ex.Message;
            LimparDashboard();
            LimparResumoExecutivo();
            ResetRececaoMaterial();
        }
        finally
        {
            IsLoadingHero = false;
        }
    }

    [RelayCommand(CanExecute = nameof(HasMoldeEntregaDashboard))]
    private async Task AbrirDashboardMoldeAsync()
    {
        if (EntregaMoldeMaisProxima is null || EntregaMoldeMaisProxima.Molde_id <= 0)
            return;

        await Shell.Current.GoToAsync($"{nameof(MoldeDetalhePage)}?molde_id={EntregaMoldeMaisProxima.Molde_id}");
    }

    [RelayCommand(CanExecute = nameof(CanRegistarChegadaMaterial))]
    private async Task RegistarChegadaMaterialAsync()
    {
        if (SelectedMoldeRececao is null)
            return;

        var pecasSelecionadas = PecasPendentesRececao
            .Where(item => item.IsSelected)
            .ToList();

        if (pecasSelecionadas.Count == 0)
            return;

        IsSavingRececaoMaterial = true;
        RececaoMaterialErrorMessage = string.Empty;

        try
        {
            foreach (var peca in pecasSelecionadas)
                await _pecasService.UpdateMaterialRecebidoAsync(peca.PecaId, materialRecebido: true);

            await _dialogService.ShowSuccessAsync(
                "Chegada registada",
                BuildRececaoSuccessMessage(SelectedMoldeRececao, pecasSelecionadas));

            await LoadAsync();
        }
        catch (Exception ex)
        {
            RececaoMaterialErrorMessage = ex.Message;
        }
        finally
        {
            IsSavingRececaoMaterial = false;
        }
    }

    private async Task CarregarHeroAsync(
        IReadOnlyCollection<EncomendaResumoDto>? encomendasEmProducao,
        IReadOnlyCollection<FilaGlobalMoldeItemDto>? filaGlobalMoldes)
    {
        if (encomendasEmProducao is null || filaGlobalMoldes is null)
        {
            HeroErrorMessage = "Nao foi possivel carregar os dados do dashboard.";
            LimparDashboard();
            return;
        }

        if (encomendasEmProducao.Count == 0)
        {
            HeroErrorMessage = "Nao existem encomendas em producao para apresentar no dashboard.";
            LimparDashboard();
            return;
        }

        var melhorCandidato = EncontrarMoldeMaisProximo(encomendasEmProducao, filaGlobalMoldes);
        if (melhorCandidato is null)
        {
            HeroErrorMessage = "Nao foi encontrada uma data de entrega valida para os moldes em producao.";
            LimparDashboard();
            return;
        }

        var moldeTask = _moldesService.GetByIdAsync(melhorCandidato.MoldeFila.MoldeId);
        var dashboardTask = _moldesService.GetDashboardCicloVidaAsync(melhorCandidato.MoldeFila.MoldeId);

        await Task.WhenAll(moldeTask, dashboardTask);

        var molde = moldeTask.Result;
        var dashboard = dashboardTask.Result;

        if (molde is null || dashboard is null)
        {
            HeroErrorMessage = "Nao foi possivel carregar o resumo do molde mais proximo de entrega.";
            LimparDashboard();
            return;
        }

        MoldeMaisProximo = molde;
        EncomendaMaisProxima = melhorCandidato.Encomenda;
        EntregaMoldeMaisProxima = new EncomendaMoldeDto
        {
            EncomendaMolde_id = melhorCandidato.MoldeFila.EncomendaMoldeId,
            Encomenda_id = melhorCandidato.MoldeFila.EncomendaId,
            Molde_id = melhorCandidato.MoldeFila.MoldeId,
            Quantidade = melhorCandidato.MoldeFila.Quantidade,
            Prioridade = melhorCandidato.MoldeFila.Prioridade,
            DataEntregaPrevista = melhorCandidato.MoldeFila.DataEntregaPrevista,
            NumeroEncomendaCliente = melhorCandidato.MoldeFila.NumeroEncomendaCliente,
            NumeroMolde = melhorCandidato.MoldeFila.NumeroMolde
        };
        DashboardMaisProximo = dashboard;
    }

    private void AtualizarResumoExecutivo(
        IReadOnlyCollection<EncomendaResumoDto>? todasEncomendas,
        IReadOnlyCollection<FilaGlobalMoldeItemDto>? filaGlobalMoldes)
    {
        if (filaGlobalMoldes is null)
        {
            TotalMoldesPorEntregar = null;
            MoldesComAtraso = null;
        }
        else
        {
            var hoje = DateTime.Today;

            TotalMoldesPorEntregar = filaGlobalMoldes.Count;
            MoldesComAtraso = filaGlobalMoldes.Count(item =>
                item.DataEntregaPrevista > DateTime.MinValue &&
                item.DataEntregaPrevista.Date < hoje);
        }

        if (todasEncomendas is null)
        {
            TaxaConclusao = null;
            EncomendasConcluidasUltimosTresMeses = null;
            return;
        }

        var hojeIntervalo = DateTime.Today;
        var inicioIntervalo = hojeIntervalo.AddMonths(-3);
        var totalEncomendas = todasEncomendas.Count;
        var totalConcluidas = todasEncomendas.Count(encomenda => IsEstado(encomenda.Estado, "CONCLUIDA"));

        TaxaConclusao = totalEncomendas == 0
            ? 0
            : decimal.Round((decimal)totalConcluidas / totalEncomendas * 100m, 2);

        EncomendasConcluidasUltimosTresMeses = todasEncomendas.Count(encomenda =>
            IsEstado(encomenda.Estado, "CONCLUIDA") &&
            encomenda.DataRegisto.Date >= inicioIntervalo &&
            encomenda.DataRegisto.Date <= hojeIntervalo);
    }

    private async Task RefreshRececaoMaterialAccessAsync()
    {
        try
        {
            CanUseRececaoMaterial = await _authorizationService.CanAccessAsync(AppFeature.Dashboard);
        }
        catch
        {
            CanUseRececaoMaterial = false;
        }
    }

    private async Task AtualizarMoldesRececaoAsync(
        IReadOnlyCollection<FilaGlobalMoldeItemDto>? filaGlobalMoldes,
        int? moldeRececaoSelecionadoId)
    {
        if (!CanUseRececaoMaterial || filaGlobalMoldes is null)
        {
            ResetRececaoMaterial();
            return;
        }

        var candidatos = filaGlobalMoldes
            .GroupBy(item => item.MoldeId)
            .Select(group => group
                .OrderBy(item => item.DataEntregaPrevista <= DateTime.MinValue ? DateTime.MaxValue : item.DataEntregaPrevista)
                .ThenBy(item => item.Prioridade)
                .First())
            .OrderBy(item => item.DataEntregaPrevista <= DateTime.MinValue ? DateTime.MaxValue : item.DataEntregaPrevista)
            .ThenBy(item => item.Prioridade)
            .ToList();

        candidatos = await FiltrarMoldesComPedidoMaterialAtivoAsync(candidatos);

        MoldesRececaoDisponiveis.Clear();

        foreach (var item in candidatos)
        {
            MoldesRececaoDisponiveis.Add(new MoldeRececaoOption
            {
                MoldeId = item.MoldeId,
                NumeroMolde = item.NumeroMolde,
                NumeroEncomendaCliente = item.NumeroEncomendaCliente,
                DataEntregaPrevista = item.DataEntregaPrevista,
                Prioridade = item.Prioridade
            });
        }

        if (MoldesRececaoDisponiveis.Count == 0)
        {
            _suppressSelectedMoldeRececaoChanged = true;
            SelectedMoldeRececao = null;
            _suppressSelectedMoldeRececaoChanged = false;
            ClearPecasPendentesRececao();
            return;
        }

        var restoredSelection = MoldesRececaoDisponiveis.FirstOrDefault(option => option.MoldeId == moldeRececaoSelecionadoId)
                                ?? MoldesRececaoDisponiveis.First();

        _suppressSelectedMoldeRececaoChanged = true;
        SelectedMoldeRececao = restoredSelection;
        _suppressSelectedMoldeRececaoChanged = false;

        if (restoredSelection is null)
        {
            ClearPecasPendentesRececao();
            return;
        }

        await LoadPecasPendentesRececaoAsync(restoredSelection.MoldeId);
    }

    private async Task<List<FilaGlobalMoldeItemDto>> FiltrarMoldesComPedidoMaterialAtivoAsync(
        IReadOnlyCollection<FilaGlobalMoldeItemDto> candidatos)
    {
        var verificacoes = candidatos.Select(async candidato =>
        {
            var pagina = await _pecasService.GetByMoldeIdPendingMaterialReceiptAsync(candidato.MoldeId, 1, 1);
            return pagina?.TotalItems > 0
                ? candidato
                : null;
        });

        var resultados = await Task.WhenAll(verificacoes);

        return resultados
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();
    }

    private async Task LoadPecasPendentesRececaoAsync(int moldeId)
    {
        if (!CanUseRececaoMaterial || moldeId <= 0)
        {
            ClearPecasPendentesRececao();
            return;
        }

        var loadVersion = ++_rececaoMaterialLoadVersion;

        IsLoadingRececaoMaterial = true;
        RececaoMaterialErrorMessage = string.Empty;

        try
        {
            var primeiraPagina = await _pecasService.GetByMoldeIdPendingMaterialReceiptAsync(moldeId, 1, 100);
            if (primeiraPagina is null)
                throw new InvalidOperationException($"Nao foi possivel carregar as pecas com pedido de material pendente do molde {moldeId}.");

            if (loadVersion != _rececaoMaterialLoadVersion || SelectedMoldeRececao?.MoldeId != moldeId)
                return;

            var pecas = primeiraPagina.Items.ToList();

            for (var page = 2; page <= primeiraPagina.TotalPages; page++)
            {
                var pagina = await _pecasService.GetByMoldeIdPendingMaterialReceiptAsync(moldeId, page, 100);
                if (pagina?.Items is null)
                    continue;

                pecas.AddRange(pagina.Items);
            }

            var pendentes = pecas
                .OrderBy(peca => peca.Prioridade)
                .ThenBy(peca => peca.NumeroPeca)
                .ThenBy(peca => peca.Designacao)
                .Select(peca => new SelectablePecaRececaoItem(peca));

            ReplacePecasPendentesRececao(pendentes);
        }
        catch (Exception ex)
        {
            if (loadVersion != _rececaoMaterialLoadVersion)
                return;

            RececaoMaterialErrorMessage = ex.Message;
            ClearPecasPendentesRececao();
        }
        finally
        {
            if (loadVersion == _rececaoMaterialLoadVersion)
                IsLoadingRececaoMaterial = false;
        }
    }

    private async Task<List<EncomendaResumoDto>?> GetAllEncomendasEmProducaoAsync()
    {
        var primeiraPagina = await _encomendasService.GetEncomendasNaoConcluidasAsync(1, 100);
        if (primeiraPagina is null)
            return null;

        var encomendas = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _encomendasService.GetEncomendasNaoConcluidasAsync(page, 100);
            if (pagina?.Items is null)
                continue;

            encomendas.AddRange(pagina.Items);
        }

        return encomendas;
    }

    private async Task<List<EncomendaResumoDto>?> GetTodasEncomendasAsync()
    {
        var primeiraPagina = await _encomendasService.GetAllAsync(1, 100);
        if (primeiraPagina is null)
            return null;

        var encomendas = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _encomendasService.GetAllAsync(page, 100);
            if (pagina?.Items is null)
                continue;

            encomendas.AddRange(pagina.Items);
        }

        return encomendas;
    }

    private async Task<List<FilaGlobalMoldeItemDto>?> GetAllFilaGlobalMoldeAsync()
    {
        var primeiraPagina = await _encomendasService.GetFilaGlobalMoldeAsync(1, 100);
        if (primeiraPagina is null)
            return null;

        var moldes = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _encomendasService.GetFilaGlobalMoldeAsync(page, 100);
            if (pagina?.Items is null)
                continue;

            moldes.AddRange(pagina.Items);
        }

        return moldes;
    }

    private static MoldeEntregaCandidato? EncontrarMoldeMaisProximo(
        IReadOnlyCollection<EncomendaResumoDto> encomendasEmProducao,
        IReadOnlyCollection<FilaGlobalMoldeItemDto> filaGlobalMoldes)
    {
        var encomendasPorId = encomendasEmProducao.ToDictionary(encomenda => encomenda.Encomenda_id);

        var melhorMolde = filaGlobalMoldes
            .Where(item =>
                item.DataEntregaPrevista > DateTime.MinValue &&
                encomendasPorId.ContainsKey(item.EncomendaId))
            .OrderBy(item => item.DataEntregaPrevista)
            .ThenBy(item => item.Prioridade)
            .FirstOrDefault();

        if (melhorMolde is null)
            return null;

        return new MoldeEntregaCandidato(encomendasPorId[melhorMolde.EncomendaId], melhorMolde);
    }

    private void ReplacePecasPendentesRececao(IEnumerable<SelectablePecaRececaoItem> pecas)
    {
        ClearPecasPendentesRececao();

        foreach (var peca in pecas)
        {
            peca.PropertyChanged += OnPecaRececaoItemPropertyChanged;
            PecasPendentesRececao.Add(peca);
        }

        NotifyRececaoMaterialStateChanged();
    }

    private void ClearPecasPendentesRececao()
    {
        foreach (var item in PecasPendentesRececao)
            item.PropertyChanged -= OnPecaRececaoItemPropertyChanged;

        PecasPendentesRececao.Clear();
        NotifyRececaoMaterialStateChanged();
    }

    private void OnPecasPendentesRececaoCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        NotifyRececaoMaterialStateChanged();
    }

    private void OnPecaRececaoItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(SelectablePecaRececaoItem.IsSelected), StringComparison.Ordinal))
            NotifyRececaoMaterialStateChanged();
    }

    private void NotifyRececaoMaterialStateChanged()
    {
        OnPropertyChanged(nameof(HasMoldesRececaoDisponiveis));
        OnPropertyChanged(nameof(HasNoMoldesRececaoDisponiveis));
        OnPropertyChanged(nameof(HasPecasPendentesRececao));
        OnPropertyChanged(nameof(HasNoPecasPendentesRececao));
        OnPropertyChanged(nameof(PecasRececaoSelectionSummary));
        OnPropertyChanged(nameof(CanRegistarChegadaMaterial));
        RegistarChegadaMaterialCommand.NotifyCanExecuteChanged();
    }

    private void ResetRececaoMaterial()
    {
        MoldesRececaoDisponiveis.Clear();

        _suppressSelectedMoldeRececaoChanged = true;
        SelectedMoldeRececao = null;
        _suppressSelectedMoldeRececaoChanged = false;

        IsLoadingRececaoMaterial = false;
        IsSavingRececaoMaterial = false;
        RececaoMaterialErrorMessage = string.Empty;
        ClearPecasPendentesRececao();
    }

    private void LimparDashboard()
    {
        MoldeMaisProximo = null;
        EncomendaMaisProxima = null;
        EntregaMoldeMaisProxima = null;
        DashboardMaisProximo = null;
    }

    private void LimparResumoExecutivo()
    {
        TotalMoldesPorEntregar = null;
        TaxaConclusao = null;
        EncomendasConcluidasUltimosTresMeses = null;
        MoldesComAtraso = null;
    }

    private static string BuildRececaoSuccessMessage(
        MoldeRececaoOption molde,
        IReadOnlyCollection<SelectablePecaRececaoItem> pecasSelecionadas)
    {
        var descricoes = pecasSelecionadas
            .Take(5)
            .Select(item => item.DesignacaoDisplay)
            .ToList();

        var listaPecas = string.Join(", ", descricoes);
        if (pecasSelecionadas.Count > descricoes.Count)
            listaPecas = $"{listaPecas} e mais {pecasSelecionadas.Count - descricoes.Count}";

        return $"Foi registada a chegada de {pecasSelecionadas.Count} peca(s) do molde {molde.NumeroMoldeDisplay}: {listaPecas}.";
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return ValorNaoDefinido;
    }

    private static bool IsEstado(string? estado, string expected)
    {
        return string.Equals(estado?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record MoldeEntregaCandidato(EncomendaResumoDto Encomenda, FilaGlobalMoldeItemDto MoldeFila);
}

public sealed class MoldeRececaoOption
{
    public int MoldeId { get; init; }
    public string NumeroMolde { get; init; } = string.Empty;
    public string NumeroEncomendaCliente { get; init; } = string.Empty;
    public DateTime DataEntregaPrevista { get; init; }
    public int Prioridade { get; init; }

    public string NumeroMoldeDisplay => string.IsNullOrWhiteSpace(NumeroMolde) ? $"Molde #{MoldeId}" : NumeroMolde;
    public string EncomendaDisplay => string.IsNullOrWhiteSpace(NumeroEncomendaCliente) ? "Sem numero" : NumeroEncomendaCliente;
    public string DataEntregaDisplay => DataEntregaPrevista > DateTime.MinValue
        ? DataEntregaPrevista.ToString("dd/MM/yyyy")
        : "Nao definida";
    public string DisplayName => $"{NumeroMoldeDisplay} | {DataEntregaDisplay}";
}

public partial class SelectablePecaRececaoItem : ObservableObject
{
    public SelectablePecaRececaoItem(PecaDto peca)
    {
        PecaId = peca.PecaId;
        NumeroPeca = peca.NumeroPeca;
        Designacao = peca.Designacao;
        Prioridade = peca.Prioridade;
        Quantidade = peca.Quantidade;
        MaterialDesignacao = peca.MaterialDesignacao;
    }

    public int PecaId { get; }
    public string NumeroPeca { get; }
    public string Designacao { get; }
    public int Prioridade { get; }
    public int Quantidade { get; }
    public string MaterialDesignacao { get; }

    [ObservableProperty]
    private bool isSelected;

    public string NumeroPecaDisplay => string.IsNullOrWhiteSpace(NumeroPeca) ? "Sem numero" : NumeroPeca;
    public string DesignacaoDisplay => string.IsNullOrWhiteSpace(Designacao) ? NumeroPecaDisplay : Designacao;
    public string MaterialDisplay => string.IsNullOrWhiteSpace(MaterialDesignacao) ? "Material nao definido" : MaterialDesignacao;
    public string QuantidadeDisplay => $"Qtd: {Quantidade}";
    public string PrioridadeDisplay => $"Prioridade {Prioridade}";
}
