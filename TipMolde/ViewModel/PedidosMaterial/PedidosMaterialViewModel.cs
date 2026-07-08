using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TipMolde.Diagnostics;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.ViewModel;

/// <summary>
/// Gere a criacao de pedidos de material, incluindo selecao de moldes, pecas e fornecedores.
/// </summary>
public partial class PedidosMaterialViewModel : ObservableObject
{
    private const int PageSize = 100;
    private const int MaxVerificacoesMoldeEmParalelo = 6;

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
    private bool _fornecedoresLoaded;
    private bool _suppressMoldesReload;

    private const int PecaPageSize = 20;

    /// <summary>
    /// Construtor do view model de pedidos de material.
    /// </summary>
    /// <param name="fornecedoresService">Servico usado para consultar e gerir fornecedores.</param>
    /// <param name="moldesService">Servico usado para carregar moldes elegiveis para pedido.</param>
    /// <param name="pecasService">Servico usado para listar pecas sem pedido de material.</param>
    /// <param name="pedidosMaterialService">Servico usado para criar os pedidos de material.</param>
    /// <param name="dialogService">Servico usado para apresentar confirmacoes e erros.</param>
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

    [ObservableProperty]
    private bool isFornecedoresVisible;

    [ObservableProperty]
    private FornecedorDto? fornecedorEmEdicao;

    [ObservableProperty]
    private string fornecedorNome = string.Empty;

    [ObservableProperty]
    private string fornecedorNif = string.Empty;

    [ObservableProperty]
    private string fornecedorMorada = string.Empty;

    [ObservableProperty]
    private string fornecedorEmail = string.Empty;

    [ObservableProperty]
    private string fornecedorTelefone = string.Empty;

    [ObservableProperty]
    private bool isSavingFornecedor;

    [ObservableProperty]
    private string fornecedoresErrorMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasMoldes => MoldesDisponiveis.Count > 0;
    public bool HasPecas => PecasDisponiveis.Count > 0;
    public bool HasFornecedores => FornecedoresDisponiveis.Count > 0;
    public bool IsEditingFornecedor => FornecedorEmEdicao is not null;
    public string FornecedoresFormTitle => IsEditingFornecedor
        ? $"Editar fornecedor {FornecedorEmEdicao?.DisplayName}"
        : "Adicionar fornecedor";
    public string FornecedoresActionText => IsEditingFornecedor ? "Atualizar fornecedor" : "Adicionar fornecedor";
    public string FornecedoresToggleText => IsFornecedoresVisible ? "Fechar fornecedores" : "Gerir fornecedores";
    public bool HasFornecedorError => !string.IsNullOrWhiteSpace(FornecedoresErrorMessage);
    public bool CanSalvarFornecedor => !IsLoading &&
                                       !IsSaving &&
                                       !IsSavingFornecedor &&
                                       !HasError &&
                                       !string.IsNullOrWhiteSpace(FornecedorNome) &&
                                       !string.IsNullOrWhiteSpace(FornecedorNif);
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
        NotifyPedidoWorkflowStateChanged();
    }

    partial void OnIsFornecedoresVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(FornecedoresToggleText));
    }

    partial void OnFornecedorEmEdicaoChanged(FornecedorDto? value)
    {
        OnPropertyChanged(nameof(IsEditingFornecedor));
        OnPropertyChanged(nameof(FornecedoresFormTitle));
        OnPropertyChanged(nameof(FornecedoresActionText));
        NotifyCanSalvarFornecedorChanged();
    }

    partial void OnFornecedorNomeChanged(string value) => NotifyCanSalvarFornecedorChanged(clearValidationError: true);

    partial void OnFornecedorNifChanged(string value) => NotifyCanSalvarFornecedorChanged(clearValidationError: true);

    partial void OnFornecedorMoradaChanged(string value) => NotifyCanSalvarFornecedorChanged(clearValidationError: true);

    partial void OnFornecedorEmailChanged(string value) => NotifyCanSalvarFornecedorChanged(clearValidationError: true);

    partial void OnFornecedorTelefoneChanged(string value) => NotifyCanSalvarFornecedorChanged(clearValidationError: true);

    partial void OnIsSavingFornecedorChanged(bool value) => NotifyCanSalvarFornecedorChanged();

    partial void OnIsLoadingChanged(bool value)
    {
        NotifyPedidoWorkflowStateChanged();
        NotifyCanSalvarFornecedorChanged();
    }

    partial void OnIsLoadingPecasChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoPecas));
        NotifyPedidoWorkflowStateChanged();
    }

    partial void OnIsSavingChanged(bool value)
    {
        NotifyPedidoWorkflowStateChanged();
        NotifyCanSalvarFornecedorChanged();
    }

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
        NotifyPedidoWorkflowStateChanged();
        NotifyCanSalvarFornecedorChanged();
    }

    partial void OnFornecedoresErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasFornecedorError));
    }

    partial void OnIsPedidoResumoVisibleChanged(bool value)
    {
        NotifyPedidoWorkflowStateChanged();
    }

    partial void OnPecaSearchTermChanged(string value)
    {
        TaskMonitor.Observe(
            "TipMolde.ViewModel.PedidosMaterial.OnPecaSearchTermChanged",
            LoadPecasAsync(resetPaging: true));
    }

    [RelayCommand]
    private async Task AtualizarDadosAsync()
    {
        await LoadAsync(forceRefresh: true);
    }

    [RelayCommand]
    private async Task ToggleFornecedoresAsync()
    {
        IsFornecedoresVisible = !IsFornecedoresVisible;

        if (!IsFornecedoresVisible)
            return;

        FornecedoresErrorMessage = string.Empty;

        try
        {
            if (!_fornecedoresLoaded)
                await RefreshFornecedoresAsync();
        }
        catch (Exception ex)
        {
            FornecedoresErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void LimparFornecedor()
    {
        ResetFornecedorForm();
    }

    [RelayCommand]
    private async Task SalvarFornecedorAsync()
    {
        if (!CanSalvarFornecedor)
            return;

        var nome = FornecedorNome.Trim();
        var nif = FornecedorNif.Trim();
        var morada = FornecedorFormDefaults.NormalizeOptional(FornecedorMorada);
        var email = FornecedorFormDefaults.NormalizeOptional(FornecedorEmail);
        var telefone = FornecedorFormDefaults.NormalizeOptional(FornecedorTelefone);

        FornecedoresErrorMessage =
            FornecedorFormDefaults.ValidateNome(nome) ??
            FornecedorFormDefaults.ValidateNif(nif) ??
            FornecedorFormDefaults.ValidateMorada(morada) ??
            FornecedorFormDefaults.ValidateEmail(email) ??
            FornecedorFormDefaults.ValidateTelefone(telefone) ??
            string.Empty;

        if (!string.IsNullOrWhiteSpace(FornecedoresErrorMessage))
            return;

        IsSavingFornecedor = true;
        FornecedoresErrorMessage = string.Empty;

        try
        {
            var fornecedorEmEdicaoId = FornecedorEmEdicao?.FornecedorId;

            if (FornecedorEmEdicao is null)
            {
                var criado = await _fornecedoresService.CreateAsync(nome, nif, morada, email, telefone);
                fornecedorEmEdicaoId = criado?.FornecedorId;

                await _dialogService.ShowSuccessAsync(
                    "Sucesso",
                    $"O fornecedor {nome} foi criado com sucesso.");
            }
            else
            {
                await _fornecedoresService.UpdateAsync(
                    FornecedorEmEdicao.FornecedorId,
                    nome,
                    nif,
                    morada,
                    email,
                    telefone);

                await _dialogService.ShowSuccessAsync(
                    "Sucesso",
                    $"O fornecedor {nome} foi atualizado com sucesso.");
            }

            ResetFornecedorForm();
            await RefreshFornecedoresAsync(fornecedorEmEdicaoId);
        }
        catch (Exception ex)
        {
            FornecedoresErrorMessage = ex.Message;
        }
        finally
        {
            IsSavingFornecedor = false;
        }
    }

    [RelayCommand]
    private void EditarFornecedor(FornecedorDto? fornecedor)
    {
        if (fornecedor is null)
            return;

        FornecedorEmEdicao = fornecedor;
        FornecedorNome = fornecedor.Nome;
        FornecedorNif = fornecedor.NIF;
        FornecedorMorada = fornecedor.Morada ?? string.Empty;
        FornecedorEmail = fornecedor.Email ?? string.Empty;
        FornecedorTelefone = fornecedor.Telefone ?? string.Empty;
        FornecedoresErrorMessage = string.Empty;
        IsFornecedoresVisible = true;
    }

    [RelayCommand]
    private async Task EliminarFornecedorAsync(FornecedorDto? fornecedor)
    {
        if (fornecedor is null)
            return;

        var confirmar = await _dialogService.ConfirmDeleteAsync($"o fornecedor {fornecedor.DisplayName}");
        if (!confirmar)
            return;

        FornecedoresErrorMessage = string.Empty;

        try
        {
            await _fornecedoresService.DeleteAsync(fornecedor.FornecedorId);

            if (FornecedorEmEdicao?.FornecedorId == fornecedor.FornecedorId)
                ResetFornecedorForm();

            await RefreshFornecedoresAsync();

            await _dialogService.ShowSuccessAsync(
                "Sucesso",
                $"O fornecedor {fornecedor.DisplayName} foi eliminado com sucesso.");
        }
        catch (Exception ex)
        {
            FornecedoresErrorMessage = ex.Message;
        }
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

    /// <summary>
    /// Carrega ou atualiza o estado base da pagina, incluindo fornecedores e moldes elegiveis.
    /// </summary>
    /// <param name="forceRefresh">Indica se o carregamento deve ignorar o estado previamente memorizado.</param>
    /// <returns>Tarefa assincrona da operacao de carregamento.</returns>
    public async Task LoadAsync(bool forceRefresh = false)
    {
        if (_loaded && !forceRefresh)
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        FornecedoresErrorMessage = string.Empty;
        var mainLoadSucceeded = true;

        var selectedFornecedorId = SelectedFornecedor?.FornecedorId;
        var selectedMoldeIds = _todosMoldes
            .Where(item => item.IsSelected)
            .Select(item => item.MoldeId)
            .ToHashSet();

        try
        {
            var fornecedoresTask = GetAllFornecedoresAsync();
            var moldesTask = GetAllMoldesAsync();
            await Task.WhenAll(fornecedoresTask, moldesTask);
            var fornecedores = await fornecedoresTask;
            var moldes = await moldesTask;

            ApplyFornecedores(fornecedores, selectedFornecedorId);

            _todosMoldes.Clear();
            _todosMoldes.AddRange(moldes.OrderBy(molde => molde.DisplayName));

            MoldesDisponiveis.Clear();
            _suppressMoldesReload = true;
            foreach (var molde in _todosMoldes)
            {
                molde.PropertyChanged += OnSelectableMoldePropertyChanged;
                molde.IsSelected = selectedMoldeIds.Contains(molde.MoldeId);
                MoldesDisponiveis.Add(molde);
            }
            _suppressMoldesReload = false;

            OnPropertyChanged(nameof(HasMoldes));
            OnPropertyChanged(nameof(SelectedMoldesCount));
            OnPropertyChanged(nameof(SelectedMoldesSummary));
            OnPropertyChanged(nameof(CanCreatePedido));
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

    private void NotifyPedidoWorkflowStateChanged()
    {
        OnPropertyChanged(nameof(CanCreatePedido));
        OnPropertyChanged(nameof(CanConfirmarResumoPedido));
        OnPropertyChanged(nameof(CanLoadMorePecas));
        CriarPedidoCommand.NotifyCanExecuteChanged();
        ConfirmarResumoPedidoCommand.NotifyCanExecuteChanged();
        CarregarMaisPecasCommand.NotifyCanExecuteChanged();
    }

    private void NotifyCanSalvarFornecedorChanged(bool clearValidationError = false)
    {
        if (clearValidationError && !string.IsNullOrWhiteSpace(FornecedoresErrorMessage))
            FornecedoresErrorMessage = string.Empty;

        OnPropertyChanged(nameof(CanSalvarFornecedor));
    }

    private async Task RefreshFornecedoresAsync(int? selectedFornecedorId = null)
    {
        var fornecedores = await GetAllFornecedoresAsync();
        ApplyFornecedores(fornecedores, selectedFornecedorId ?? SelectedFornecedor?.FornecedorId);
    }

    private void ApplyFornecedores(IEnumerable<FornecedorDto> fornecedores, int? selectedFornecedorId)
    {
        _todosFornecedores.Clear();
        _todosFornecedores.AddRange(fornecedores.OrderBy(fornecedor => fornecedor.Nome));

        FornecedoresDisponiveis.Clear();
        foreach (var fornecedor in _todosFornecedores)
            FornecedoresDisponiveis.Add(fornecedor);

        _fornecedoresLoaded = true;
        OnPropertyChanged(nameof(HasFornecedores));

        SelectedFornecedor = ResolveFornecedor(selectedFornecedorId);
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
        var searchSnapshot = PecaSearchTerm?.Trim();

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

                    var pagina = await GetPecasPageAsync(molde.MoldeId, state.NextPage, PecaPageSize, searchSnapshot);
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
            RefreshPecasDisponiveis(searchSnapshot);
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
        var primeiraPagina = await _moldesService.GetComEncomendaAsync(null, 1, PageSize);
        if (primeiraPagina is null)
            throw new InvalidOperationException("Nao foi possivel carregar os moldes.");

        var moldes = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _moldesService.GetComEncomendaAsync(null, page, PageSize);
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

        using var semaphore = new SemaphoreSlim(MaxVerificacoesMoldeEmParalelo);
        var verificacoes = await Task.WhenAll(ordenados.Select(molde => BuildMoldeElegivelAsync(molde, semaphore)));

        var moldesElegiveis = verificacoes
            .Where(result => result.Item is not null)
            .Select(result => result.Item!)
            .OrderBy(item => item.DisplayName)
            .ToList();

        if (moldesElegiveis.Count == 0 && verificacoes.Any(result => result.Failed))
            throw new InvalidOperationException("Nao foi possivel validar os moldes com pecas elegiveis para pedido de material.");

        return moldesElegiveis;
    }

    private async Task<PagedResult<PecaDto>?> GetPecasPageAsync(int moldeId, int page, int pageSize, string? searchTerm = null)
    {
        return await _pecasService.GetByMoldeIdWithoutPedidoMaterialAsync(moldeId, page, pageSize, searchTerm);
    }

    private FornecedorDto? ResolveFornecedor(int? fornecedorId)
    {
        if (fornecedorId is null)
            return FornecedoresDisponiveis.FirstOrDefault();

        return FornecedoresDisponiveis.FirstOrDefault(item => item.FornecedorId == fornecedorId)
            ?? FornecedoresDisponiveis.FirstOrDefault();
    }

    private void ResetFornecedorForm()
    {
        FornecedorEmEdicao = null;
        FornecedorNome = string.Empty;
        FornecedorNif = string.Empty;
        FornecedorMorada = string.Empty;
        FornecedorEmail = string.Empty;
        FornecedorTelefone = string.Empty;
        FornecedoresErrorMessage = string.Empty;
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

    private void RefreshPecasDisponiveis(string? searchTerm = null)
    {
        PecasDisponiveis.Clear();

        var ordenadas = _todosPecas
            .OrderBy(item => item.MoldeNumero)
            .ThenBy(item => item.Prioridade)
            .ThenBy(item => item.NumeroPeca)
            .ThenBy(item => item.Designacao);

        foreach (var peca in ordenadas)
            PecasDisponiveis.Add(peca);

        if (_todosPecas.Count == 0)
        {
            PecaInfoMessage = SelectedMoldesCount == 0
                ? "Seleciona um ou mais moldes para ver as pecas sem pedido de material."
                : !string.IsNullOrWhiteSpace(searchTerm)
                    ? "Nenhuma peca corresponde a pesquisa."
                    : "Nao existem pecas sem pedido de material nos moldes selecionados.";
        }
        else if (!string.IsNullOrWhiteSpace(searchTerm))
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

        TaskMonitor.Observe(
            "TipMolde.ViewModel.PedidosMaterial.OnSelectableMoldePropertyChanged",
            LoadPecasAsync(resetPaging: true));
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

    private async Task<MoldeElegivelCheckResult> BuildMoldeElegivelAsync(MoldeDto molde, SemaphoreSlim semaphore)
    {
        var semaphoreAdquirido = false;

        try
        {
            await semaphore.WaitAsync();
            semaphoreAdquirido = true;

            var pagina = await _pecasService.GetByMoldeIdWithoutPedidoMaterialAsync(molde.MoldeId, 1, 1);
            if (pagina is null)
                return new MoldeElegivelCheckResult(null, Failed: true);

            return pagina.TotalItems > 0
                ? new MoldeElegivelCheckResult(new SelectableMoldePedidoMaterialItem(molde, pagina.TotalItems), Failed: false)
                : new MoldeElegivelCheckResult(null, Failed: false);
        }
        catch (Exception ex)
        {
            TaskMonitor.ReportException(
                $"TipMolde.ViewModel.PedidosMaterial.BuildMoldeElegivelAsync.MoldeId={molde.MoldeId}",
                ex);
            return new MoldeElegivelCheckResult(null, Failed: true);
        }
        finally
        {
            if (semaphoreAdquirido)
                semaphore.Release();
        }
    }

    private sealed record MoldeElegivelCheckResult(SelectableMoldePedidoMaterialItem? Item, bool Failed);

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
        this.totalPecasDisponiveis = totalPecasDisponiveis;
    }

    public MoldeDto Molde { get; }

    public int MoldeId => Molde.MoldeId;

    public string DisplayName => Molde.DisplayName;

    public string NumeroDisplay => Molde.Numero;

    public string NomeDisplay => Molde.Nome;

    public string ResumoDisplay => TotalPecasDisponiveis switch
    {
        null => "Seleciona para carregar pecas elegiveis.",
        0 => "Sem pecas elegiveis para pedido.",
        1 => "1 peca disponivel",
        _ => $"{TotalPecasDisponiveis} pecas disponiveis"
    };

    public string TotalPecasDisponiveisDisplay => TotalPecasDisponiveis.HasValue
        ? $"Pecas: {TotalPecasDisponiveis.Value}"
        : "Pecas: por carregar";

    public string ImagemCapaSource => Molde.ImagemCapaSource;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private int? totalPecasDisponiveis;

    partial void OnTotalPecasDisponiveisChanged(int? value)
    {
        OnPropertyChanged(nameof(ResumoDisplay));
        OnPropertyChanged(nameof(TotalPecasDisponiveisDisplay));
    }
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
        ? "Peca sem numero"
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
