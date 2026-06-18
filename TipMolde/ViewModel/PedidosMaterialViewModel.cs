using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class PedidosMaterialViewModel : ObservableObject
{
    private const int PageSize = 100;

    private readonly FornecedoresService _fornecedoresService;
    private readonly MoldesService _moldesService;
    private readonly PecasService _pecasService;
    private readonly PedidosMaterialService _pedidosMaterialService;
    private readonly IDialogService _dialogService;
    private readonly List<FornecedorDto> _todosFornecedores = [];
    private readonly List<SelectableMoldePedidoMaterialItem> _todosMoldes = [];
    private readonly List<SelectablePecaPedidoMaterialItem> _todosPecas = [];
    private readonly Dictionary<int, PecaPagingState> _pecaPagingStates = [];
    private int _pecasLoadVersion;
    private bool _loaded;
    private bool _suppressMoldesReload;

    private const int PecaPageSize = 20;

    public PedidosMaterialViewModel(
        FornecedoresService fornecedoresService,
        MoldesService moldesService,
        PecasService pecasService,
        PedidosMaterialService pedidosMaterialService,
        IDialogService dialogService)
    {
        _fornecedoresService = fornecedoresService;
        _moldesService = moldesService;
        _pecasService = pecasService;
        _pedidosMaterialService = pedidosMaterialService;
        _dialogService = dialogService;
    }

    public ObservableCollection<FornecedorDto> FornecedoresDisponiveis { get; } = new();

    public ObservableCollection<SelectableMoldePedidoMaterialItem> MoldesDisponiveis { get; } = new();

    public ObservableCollection<SelectablePecaPedidoMaterialItem> PecasDisponiveis { get; } = new();

    [ObservableProperty]
    private FornecedorDto? selectedFornecedor;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isLoadingPecas;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string pecaInfoMessage = "Seleciona um ou mais moldes para ver as pecas sem pedido de material.";

    [ObservableProperty]
    private bool isPedidoResumoVisible;

    [ObservableProperty]
    private string pedidoResumoTitulo = "Resumo do pedido de material";

    [ObservableProperty]
    private string pedidoResumoTexto = string.Empty;

    [ObservableProperty]
    private string pecaSearchTerm = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasMoldes => MoldesDisponiveis.Count > 0;
    public bool HasPecas => PecasDisponiveis.Count > 0;
    public bool HasNoPecas => SelectedMoldesCount > 0 && !IsLoadingPecas && !HasError && _todosPecas.Count == 0;
    public bool HasMorePecas => _pecaPagingStates.Values.Any(state => state.HasMore);
    public int SelectedMoldesCount => _todosMoldes.Count(item => item.IsSelected);
    public string SelectedMoldesSummary => SelectedMoldesCount switch
    {
        0 => "Nenhum molde selecionado.",
        1 => "1 molde selecionado.",
        _ => $"{SelectedMoldesCount} moldes selecionados."
    };
    public int SelectedPecasCount => _todosPecas.Count(item => item.IsSelected);
    public bool SelectedPecasHaveValidQuantity => _todosPecas
        .Where(item => item.IsSelected)
        .All(item => item.QuantidadePedido > 0);
    public string SelectedPecasSummary => SelectedPecasCount switch
    {
        0 => "Nenhuma peca selecionada.",
        1 => "1 peca selecionada.",
        _ => $"{SelectedPecasCount} pecas selecionadas."
    };
    public bool CanCreatePedido => !IsLoading &&
                                   !IsLoadingPecas &&
                                   !IsSaving &&
                                   !IsPedidoResumoVisible &&
                                   !HasError &&
                                   SelectedFornecedor is not null &&
                                   SelectedMoldesCount > 0 &&
                                   SelectedPecasCount > 0 &&
                                   SelectedPecasHaveValidQuantity;

    public bool CanLoadMorePecas => !IsLoading &&
                                    !IsLoadingPecas &&
                                    !IsSaving &&
                                    !HasError &&
                                    SelectedMoldesCount > 0 &&
                                    HasMorePecas;

    public bool CanConfirmarResumoPedido => IsPedidoResumoVisible &&
                                            !IsLoading &&
                                            !IsLoadingPecas &&
                                            !IsSaving &&
                                            !HasError &&
                                            SelectedFornecedor is not null &&
                                            SelectedMoldesCount > 0 &&
                                            SelectedPecasCount > 0 &&
                                            SelectedPecasHaveValidQuantity;

    partial void OnSelectedFornecedorChanged(FornecedorDto? value)
    {
        OnPropertyChanged(nameof(CanCreatePedido));
        CriarPedidoCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCreatePedido));
        OnPropertyChanged(nameof(CanLoadMorePecas));
        CriarPedidoCommand.NotifyCanExecuteChanged();
        CarregarMaisPecasCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingPecasChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoPecas));
        OnPropertyChanged(nameof(CanCreatePedido));
        OnPropertyChanged(nameof(CanLoadMorePecas));
        CriarPedidoCommand.NotifyCanExecuteChanged();
        CarregarMaisPecasCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSavingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCreatePedido));
        OnPropertyChanged(nameof(CanLoadMorePecas));
        CriarPedidoCommand.NotifyCanExecuteChanged();
        CarregarMaisPecasCommand.NotifyCanExecuteChanged();
    }

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(CanCreatePedido));
        OnPropertyChanged(nameof(CanConfirmarResumoPedido));
        OnPropertyChanged(nameof(CanLoadMorePecas));
        CriarPedidoCommand.NotifyCanExecuteChanged();
        ConfirmarResumoPedidoCommand.NotifyCanExecuteChanged();
        CarregarMaisPecasCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsPedidoResumoVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCreatePedido));
        OnPropertyChanged(nameof(CanConfirmarResumoPedido));
        OnPropertyChanged(nameof(CanLoadMorePecas));
        CriarPedidoCommand.NotifyCanExecuteChanged();
        ConfirmarResumoPedidoCommand.NotifyCanExecuteChanged();
        CarregarMaisPecasCommand.NotifyCanExecuteChanged();
    }

    partial void OnPecaSearchTermChanged(string value)
    {
        ApplyPecaFilter();
    }

    [RelayCommand]
    private async Task AtualizarDadosAsync()
    {
        await LoadAsync(forceRefresh: true);
    }

    [RelayCommand(CanExecute = nameof(CanCreatePedido))]
    private Task CriarPedidoAsync()
    {
        if (SelectedFornecedor is null || SelectedMoldesCount <= 0)
            return Task.CompletedTask;

        var pecasSelecionadas = GetPecasSelecionadas();
        if (pecasSelecionadas.Count == 0)
            return Task.CompletedTask;

        PedidoResumoTitulo = "Resumo do pedido de material";
        PedidoResumoTexto = BuildPedidoResumo(SelectedFornecedor, pecasSelecionadas);
        IsPedidoResumoVisible = true;
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanConfirmarResumoPedido))]
    private async Task ConfirmarResumoPedidoAsync()
    {
        if (SelectedFornecedor is null || SelectedMoldesCount <= 0)
            return;

        var pecasSelecionadas = GetPecasSelecionadas();
        if (pecasSelecionadas.Count == 0)
            return;

        var linhas = pecasSelecionadas
            .Select(item => new CreatePedidoMaterialItemRequest
            {
                Peca_id = item.PecaId,
                Quantidade = item.QuantidadePedido
            })
            .ToList();

        IsSaving = true;
        ErrorMessage = string.Empty;

        try
        {
            var pedido = await _pedidosMaterialService.CreateAsync(new CreatePedidoMaterialRequest
            {
                Fornecedor_id = SelectedFornecedor.FornecedorId,
                Itens = linhas
            });

            if (pedido is null)
                throw new InvalidOperationException("Nao foi possivel criar o pedido de material.");

            FecharResumoPedido();

            await _dialogService.ShowSuccessAsync(
                "Pedido criado",
                BuildSuccessMessage(SelectedFornecedor, pecasSelecionadas.Count, SelectedMoldesCount));

            await LoadAsync(forceRefresh: true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void CancelarResumoPedido()
    {
        FecharResumoPedido();
    }

    [RelayCommand]
    private void ContinuarSelecionarPedido()
    {
        FecharResumoPedido();
    }

    [RelayCommand(CanExecute = nameof(CanLoadMorePecas))]
    private async Task CarregarMaisPecasAsync()
    {
        await LoadPecasAsync(resetPaging: false);
    }

    public async Task LoadAsync(bool forceRefresh = false)
    {
        if (_loaded && !forceRefresh)
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        var mainLoadSucceeded = true;

        var selectedFornecedorId = SelectedFornecedor?.FornecedorId;

        try
        {
            var fornecedoresTask = GetAllFornecedoresAsync();
            var moldesTask = GetAllMoldesAsync();
            await Task.WhenAll(fornecedoresTask, moldesTask);

            _todosFornecedores.Clear();
            _todosFornecedores.AddRange(fornecedoresTask.Result.OrderBy(fornecedor => fornecedor.Nome));

            FornecedoresDisponiveis.Clear();
            foreach (var fornecedor in _todosFornecedores)
                FornecedoresDisponiveis.Add(fornecedor);

            _todosMoldes.Clear();
            _todosMoldes.AddRange(moldesTask.Result.OrderBy(molde => molde.DisplayName));

            MoldesDisponiveis.Clear();
            _suppressMoldesReload = true;
            foreach (var molde in _todosMoldes)
            {
                molde.PropertyChanged += OnSelectableMoldePropertyChanged;
                MoldesDisponiveis.Add(molde);
            }
            _suppressMoldesReload = false;

            OnPropertyChanged(nameof(HasMoldes));
            OnPropertyChanged(nameof(SelectedMoldesCount));
            OnPropertyChanged(nameof(SelectedMoldesSummary));
            OnPropertyChanged(nameof(CanCreatePedido));

            SelectedFornecedor = ResolveFornecedor(selectedFornecedorId);
            _loaded = true;
        }
        catch (Exception ex)
        {
            mainLoadSucceeded = false;
            ErrorMessage = ex.Message;
            ClearPecasDisponiveis();
        }
        finally
        {
            IsLoading = false;
        }

        if (mainLoadSucceeded)
            await LoadPecasAsync(resetPaging: true);
        else
            ClearPecasDisponiveis();
    }

    private async Task LoadPecasAsync(bool resetPaging)
    {
        var moldesSelecionados = _todosMoldes
            .Where(item => item.IsSelected)
            .ToList();

        if (moldesSelecionados.Count == 0)
        {
            ClearPecasDisponiveis();
            return;
        }

        var loadVersion = ++_pecasLoadVersion;
        var selectedPecasIds = _todosPecas
            .Where(item => item.IsSelected)
            .Select(item => item.PecaId)
            .ToHashSet();

        IsLoadingPecas = true;
        ErrorMessage = string.Empty;
        PecaInfoMessage = resetPaging
            ? "A carregar pecas elegiveis..."
            : "A carregar mais pecas...";

        try
        {
            if (resetPaging)
            {
                ResetPecaPagingState(moldesSelecionados);
                ClearPecasDisponiveis(clearPagingStates: false);
            }

            var carregamentos = await Task.WhenAll(moldesSelecionados
                .Where(molde => !_pecaPagingStates.TryGetValue(molde.MoldeId, out var state) || state.HasMore)
                .Select(async molde =>
                {
                    if (!_pecaPagingStates.TryGetValue(molde.MoldeId, out var state))
                        return Array.Empty<SelectablePecaPedidoMaterialItem>();

                    var pagina = await GetPecasPageAsync(molde.MoldeId, state.NextPage, PecaPageSize);
                    if (pagina is null)
                        throw new InvalidOperationException($"Nao foi possivel carregar as pecas do molde {molde.MoldeId}.");

                    state.NextPage++;
                    state.HasMore = state.NextPage <= pagina.TotalPages;

                    return pagina.Items
                        .Select(peca => new SelectablePecaPedidoMaterialItem(
                            peca,
                            molde.DisplayName,
                            selectedPecasIds.Contains(peca.PecaId)))
                        .ToArray();
                }));

            if (loadVersion != _pecasLoadVersion)
                return;

            var novasPecas = carregamentos.SelectMany(item => item).ToList();
            AppendPecasDisponiveis(novasPecas);
            ApplyPecaFilter();
        }
        catch (Exception ex)
        {
            if (loadVersion != _pecasLoadVersion)
                return;

            ErrorMessage = ex.Message;
            ClearPecasDisponiveis();
            PecaInfoMessage = "Nao foi possivel carregar as pecas elegiveis.";
        }
        finally
        {
            if (loadVersion == _pecasLoadVersion)
                IsLoadingPecas = false;
        }
    }

    private async Task<List<FornecedorDto>> GetAllFornecedoresAsync()
    {
        var primeiraPagina = await _fornecedoresService.GetAllAsync(1, PageSize);
        if (primeiraPagina is null)
            throw new InvalidOperationException("Nao foi possivel carregar os fornecedores.");

        var fornecedores = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _fornecedoresService.GetAllAsync(page, PageSize);
            if (pagina?.Items is null)
                continue;

            fornecedores.AddRange(pagina.Items);
        }

        return fornecedores;
    }

    private async Task<List<SelectableMoldePedidoMaterialItem>> GetAllMoldesAsync()
    {
        var primeiraPagina = await _moldesService.GetAllAsync(1, PageSize);
        if (primeiraPagina is null)
            throw new InvalidOperationException("Nao foi possivel carregar os moldes.");

        var moldes = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _moldesService.GetAllAsync(page, PageSize);
            if (pagina?.Items is null)
                continue;

            moldes.AddRange(pagina.Items);
        }

        var ordenados = moldes
            .GroupBy(molde => molde.MoldeId)
            .Select(group => group.First())
            .OrderBy(molde => molde.Numero)
            .ThenBy(molde => molde.Nome)
            .ToList();

        var moldesComPecas = await Task.WhenAll(ordenados.Select(async molde =>
        {
            var pagina = await _pecasService.GetByMoldeIdWithoutPedidoMaterialAsync(molde.MoldeId, 1, 1);
            var totalPecas = pagina?.TotalItems ?? 0;

            return totalPecas <= 0
                ? null
                : new SelectableMoldePedidoMaterialItem(molde, totalPecas);
        }));

        return moldesComPecas
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();
    }

    private async Task<PagedResult<PecaDto>?> GetPecasPageAsync(int moldeId, int page, int pageSize)
    {
        return await _pecasService.GetByMoldeIdWithoutPedidoMaterialAsync(moldeId, page, pageSize);
    }

    private FornecedorDto? ResolveFornecedor(int? fornecedorId)
    {
        if (fornecedorId is null)
            return FornecedoresDisponiveis.FirstOrDefault();

        return FornecedoresDisponiveis.FirstOrDefault(item => item.FornecedorId == fornecedorId)
            ?? FornecedoresDisponiveis.FirstOrDefault();
    }

    private void ResetPecaPagingState(IEnumerable<SelectableMoldePedidoMaterialItem> moldesSelecionados)
    {
        _pecaPagingStates.Clear();

        foreach (var molde in moldesSelecionados)
        {
            _pecaPagingStates[molde.MoldeId] = new PecaPagingState();
        }
    }

    private void AppendPecasDisponiveis(IEnumerable<SelectablePecaPedidoMaterialItem> pecas)
    {
        foreach (var peca in pecas)
        {
            peca.PropertyChanged += OnSelectablePecaPropertyChanged;
            _todosPecas.Add(peca);
        }
    }

    private void ClearPecasDisponiveis(bool clearPagingStates = true)
    {
        foreach (var peca in _todosPecas)
            peca.PropertyChanged -= OnSelectablePecaPropertyChanged;

        _todosPecas.Clear();
        PecasDisponiveis.Clear();
        if (clearPagingStates)
            _pecaPagingStates.Clear();
        PecaInfoMessage = SelectedMoldesCount == 0
            ? "Seleciona um ou mais moldes para ver as pecas sem pedido de material."
            : "Nao existem pecas sem pedido de material nos moldes selecionados.";

        OnPropertyChanged(nameof(HasPecas));
        OnPropertyChanged(nameof(HasNoPecas));
        OnPropertyChanged(nameof(HasMorePecas));
        OnPropertyChanged(nameof(SelectedPecasCount));
        OnPropertyChanged(nameof(SelectedPecasSummary));
        OnPropertyChanged(nameof(SelectedPecasHaveValidQuantity));
        OnPropertyChanged(nameof(CanCreatePedido));
        OnPropertyChanged(nameof(CanConfirmarResumoPedido));
        OnPropertyChanged(nameof(CanLoadMorePecas));
        CriarPedidoCommand.NotifyCanExecuteChanged();
        ConfirmarResumoPedidoCommand.NotifyCanExecuteChanged();
        CarregarMaisPecasCommand.NotifyCanExecuteChanged();
    }

    private void ApplyPecaFilter()
    {
        var search = PecaSearchTerm?.Trim();

        PecasDisponiveis.Clear();

        var filtradas = _todosPecas
            .Where(item => string.IsNullOrWhiteSpace(search) ||
                           Contains(item.NumeroPeca, search) ||
                           Contains(item.Designacao, search) ||
                           Contains(item.MoldeDisplay, search) ||
                           Contains(item.MaterialDesignacao, search) ||
                           Contains(item.TratamentoTermico, search) ||
                           Contains(item.Observacao, search))
            .OrderBy(item => item.MoldeNumero)
            .ThenBy(item => item.Prioridade)
            .ThenBy(item => item.NumeroPeca)
            .ThenBy(item => item.Designacao);

        foreach (var peca in filtradas)
            PecasDisponiveis.Add(peca);

        if (_todosPecas.Count == 0)
        {
            PecaInfoMessage = SelectedMoldesCount == 0
                ? "Seleciona um ou mais moldes para ver as pecas sem pedido de material."
                : "Nao existem pecas sem pedido de material nos moldes selecionados.";
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            PecaInfoMessage = PecasDisponiveis.Count == 0
                ? "Nenhuma peca corresponde a pesquisa."
                : $"{PecasDisponiveis.Count} peca(s) correspondem a pesquisa.";
        }
        else
        {
            PecaInfoMessage = $"{PecasDisponiveis.Count} peca(s) disponiveis para pedido em {SelectedMoldesCount} molde(s).";
        }

        OnPropertyChanged(nameof(HasPecas));
        OnPropertyChanged(nameof(HasNoPecas));
        OnPropertyChanged(nameof(SelectedPecasCount));
        OnPropertyChanged(nameof(SelectedPecasSummary));
        OnPropertyChanged(nameof(SelectedPecasHaveValidQuantity));
        OnPropertyChanged(nameof(CanCreatePedido));
        OnPropertyChanged(nameof(CanConfirmarResumoPedido));
        OnPropertyChanged(nameof(CanLoadMorePecas));
        OnPropertyChanged(nameof(HasMorePecas));
        CriarPedidoCommand.NotifyCanExecuteChanged();
        ConfirmarResumoPedidoCommand.NotifyCanExecuteChanged();
        CarregarMaisPecasCommand.NotifyCanExecuteChanged();
    }

    private static bool Contains(string? source, string search)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               source.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private void OnSelectablePecaPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(SelectablePecaPedidoMaterialItem.IsSelected) &&
            e.PropertyName is not nameof(SelectablePecaPedidoMaterialItem.QuantidadeTexto) &&
            e.PropertyName is not nameof(SelectablePecaPedidoMaterialItem.QuantidadePedido))
        {
            return;
        }

        OnPropertyChanged(nameof(SelectedPecasCount));
        OnPropertyChanged(nameof(SelectedPecasSummary));
        OnPropertyChanged(nameof(SelectedPecasHaveValidQuantity));
        OnPropertyChanged(nameof(CanCreatePedido));
        OnPropertyChanged(nameof(CanLoadMorePecas));
        CriarPedidoCommand.NotifyCanExecuteChanged();
        CarregarMaisPecasCommand.NotifyCanExecuteChanged();
    }

    private void OnSelectableMoldePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(SelectableMoldePedidoMaterialItem.IsSelected))
            return;

        OnPropertyChanged(nameof(HasNoPecas));
        OnPropertyChanged(nameof(SelectedMoldesCount));
        OnPropertyChanged(nameof(SelectedMoldesSummary));
        OnPropertyChanged(nameof(CanCreatePedido));
        OnPropertyChanged(nameof(CanConfirmarResumoPedido));
        CriarPedidoCommand.NotifyCanExecuteChanged();
        ConfirmarResumoPedidoCommand.NotifyCanExecuteChanged();

        if (_suppressMoldesReload)
            return;

        _ = LoadPecasAsync(resetPaging: true);
    }

    private static string BuildSuccessMessage(FornecedorDto fornecedor, int linhas, int moldes)
    {
        var moldesTexto = moldes == 1 ? "1 molde" : $"{moldes} moldes";
        var linhasTexto = linhas == 1 ? "1 linha" : $"{linhas} linhas";

        return $"Pedido de material criado para {fornecedor.DisplayName} com {linhasTexto} repartidas por {moldesTexto}.";
    }

    private static string BuildPedidoResumo(FornecedorDto fornecedor, IReadOnlyCollection<SelectablePecaPedidoMaterialItem> pecasSelecionadas)
    {
        var moldes = pecasSelecionadas
            .Select(item => item.MoldeDisplay)
            .Distinct()
            .OrderBy(item => item)
            .ToList();

        var linhas = pecasSelecionadas
            .OrderBy(item => item.MoldeNumero)
            .ThenBy(item => item.NumeroPeca)
            .ThenBy(item => item.Designacao)
            .Select(item => $" - {item.MoldeDisplay} | {item.NumeroPeca} | {item.Designacao} | qtd {item.QuantidadePedido}")
            .ToList();

        var resumo = new List<string>
        {
            $"Fornecedor: {fornecedor.DisplayName}",
            $"Moldes selecionados: {string.Join(", ", moldes)}",
            $"Total de linhas: {pecasSelecionadas.Count}",
            "Pecas selecionadas:"
        };

        resumo.AddRange(linhas);

        return string.Join(Environment.NewLine, resumo);
    }

    private IReadOnlyCollection<SelectablePecaPedidoMaterialItem> GetPecasSelecionadas()
    {
        return _todosPecas
            .Where(item => item.IsSelected)
            .ToList();
    }

    private void FecharResumoPedido()
    {
        IsPedidoResumoVisible = false;
    }

    private sealed class PecaPagingState
    {
        public int NextPage { get; set; } = 1;

        public bool HasMore { get; set; } = true;
    }
}

public sealed partial class SelectableMoldePedidoMaterialItem : ObservableObject
{
    public SelectableMoldePedidoMaterialItem(MoldeDto molde, int totalPecasDisponiveis)
    {
        Molde = molde;
        TotalPecasDisponiveis = totalPecasDisponiveis;
    }

    public MoldeDto Molde { get; }

    public int MoldeId => Molde.MoldeId;

    public int TotalPecasDisponiveis { get; }

    public string DisplayName => Molde.DisplayName;

    public string NumeroDisplay => Molde.Numero;

    public string NomeDisplay => Molde.Nome;

    public string ResumoDisplay => TotalPecasDisponiveis switch
    {
        1 => "1 peca disponivel",
        _ => $"{TotalPecasDisponiveis} pecas disponiveis"
    };

    public string ImagemCapaSource => Molde.ImagemCapaSource;

    [ObservableProperty]
    private bool isSelected;
}

public sealed partial class SelectablePecaPedidoMaterialItem : ObservableObject
{
    private readonly PecaDto _peca;

    public SelectablePecaPedidoMaterialItem(PecaDto peca, string moldeDisplay, bool isSelected = false)
    {
        _peca = peca;
        MoldeDisplay = moldeDisplay;
        quantidadeTexto = Math.Max(1, peca.Quantidade).ToString(CultureInfo.CurrentCulture);
        IsSelected = isSelected;
    }

    public int PecaId => _peca.PecaId;

    public int MoldeId => _peca.Molde_id;

    public string MoldeDisplay { get; }

    public string MoldeNumero => MoldeDisplay;

    public string NumeroPeca => string.IsNullOrWhiteSpace(_peca.NumeroPeca)
        ? $"Peca #{_peca.PecaId}"
        : _peca.NumeroPeca;

    public string Designacao => _peca.Designacao;

    public int Prioridade => _peca.Prioridade;

    public string Resumo => $"Molde {MoldeDisplay} | Prioridade {_peca.Prioridade} | Qtd. da peca {_peca.Quantidade} | Fase {GetProximaFaseLabel(_peca.ProximaFaseNome)}";

    public string MaterialDesignacao => string.IsNullOrWhiteSpace(_peca.MaterialDesignacao)
        ? "Material nao definido"
        : _peca.MaterialDesignacao;

    public string TratamentoTermico => string.IsNullOrWhiteSpace(_peca.TratamentoTermico)
        ? "Tratamento termico nao definido"
        : _peca.TratamentoTermico;

    public string Massa => string.IsNullOrWhiteSpace(_peca.Massa)
        ? "Massa nao definida"
        : _peca.Massa;

    public string Observacao => string.IsNullOrWhiteSpace(_peca.Observacao)
        ? "Sem observacoes"
        : _peca.Observacao;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private string quantidadeTexto;

    public int QuantidadePedido =>
        int.TryParse(QuantidadeTexto, NumberStyles.Integer, CultureInfo.CurrentCulture, out var quantidade) && quantidade > 0
            ? quantidade
            : 0;

    public bool IsQuantidadeValida => QuantidadePedido > 0;

    partial void OnQuantidadeTextoChanged(string value)
    {
        OnPropertyChanged(nameof(QuantidadePedido));
        OnPropertyChanged(nameof(IsQuantidadeValida));
    }

    private static string GetProximaFaseLabel(string? proximaFaseNome)
    {
        return string.IsNullOrWhiteSpace(proximaFaseNome)
            ? "N/A"
            : proximaFaseNome;
    }
}
