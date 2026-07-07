using CommunityToolkit.Mvvm.Input;
using System.Collections.Specialized;
using System.ComponentModel;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.ViewModel;

/// <summary>
/// Concentra o fluxo de rececao de material dentro do dashboard.
/// </summary>
public partial class DashboardViewModel
{
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

    /// <summary>
    /// Regista a chegada de material para as pecas selecionadas do molde atual.
    /// </summary>
    /// <returns>Tarefa assincrona da operacao de rececao.</returns>
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

        var opcoes = candidatos
            .Select(BuildMoldeRececaoOption)
            .ToList();

        var moldesPendentes = await ObterMoldesRececaoDePedidosPendentesAsync(filaGlobalMoldes);
        foreach (var opcao in moldesPendentes)
        {
            if (opcoes.Any(item => item.MoldeId == opcao.MoldeId))
                continue;

            opcoes.Add(opcao);
        }

        opcoes = opcoes
            .OrderBy(item => item.DataEntregaPrevista <= DateTime.MinValue ? DateTime.MaxValue : item.DataEntregaPrevista)
            .ThenBy(item => item.Prioridade)
            .ThenBy(item => item.NumeroMoldeDisplay)
            .Take(MaxMoldesRececaoInicial)
            .ToList();

        MoldesRececaoDisponiveis.Clear();
        foreach (var item in opcoes)
            MoldesRececaoDisponiveis.Add(item);

        var moldeSelecionado = MoldesRececaoDisponiveis.FirstOrDefault(item => item.MoldeId == moldeRececaoSelecionadoId);

        _suppressSelectedMoldeRececaoChanged = true;
        SelectedMoldeRececao = moldeSelecionado;
        _suppressSelectedMoldeRececaoChanged = false;

        if (SelectedMoldeRececao is null)
        {
            ClearPecasPendentesRececao();
            return;
        }

        await LoadPecasPendentesRececaoAsync(SelectedMoldeRececao.MoldeId);
    }

    private async Task<List<FilaGlobalMoldeItemDto>> FiltrarMoldesComPedidoMaterialAtivoAsync(
        IReadOnlyCollection<FilaGlobalMoldeItemDto> candidatos)
    {
        using var semaphore = new SemaphoreSlim(MaxVerificacoesRececaoEmParalelo);
        var tarefas = candidatos
            .Select(candidato => VerificarMoldeComPedidoMaterialAtivoAsync(candidato, semaphore))
            .ToList();

        var resultados = await Task.WhenAll(tarefas);
        return resultados
            .Where(item => item is not null)
            .Select(item => item!)
            .Take(MaxMoldesRececaoInicial)
            .ToList();
    }

    private async Task<List<MoldeRececaoOption>> ObterMoldesRececaoDePedidosPendentesAsync(
        IReadOnlyCollection<FilaGlobalMoldeItemDto>? filaGlobalMoldes)
    {
        var pedidos = await GetAllPedidosMaterialAsync();
        if (pedidos.Count == 0)
            return [];

        var pecaIdsPendentes = pedidos
            .Where(item => string.Equals(item.Estado, "PENDENTE", StringComparison.OrdinalIgnoreCase))
            .SelectMany(item => item.Itens)
            .Select(item => item.PecaId)
            .Distinct()
            .ToList();

        if (pecaIdsPendentes.Count == 0)
            return [];

        using var semaphore = new SemaphoreSlim(MaxVerificacoesRececaoEmParalelo);
        var tarefas = pecaIdsPendentes
            .Select(pecaId => GetPecaByIdSafeAsync(pecaId, semaphore))
            .ToList();

        var pecas = (await Task.WhenAll(tarefas))
            .Where(item => item is not null && !item.MaterialRecebido)
            .Select(item => item!)
            .ToList();

        if (pecas.Count == 0)
            return [];

        var filaPorMoldeId = filaGlobalMoldes?
            .GroupBy(item => item.MoldeId)
            .ToDictionary(group => group.Key, group => group.First())
            ?? [];

        var moldesPendentes = new List<MoldeRececaoOption>();
        foreach (var grupo in pecas.GroupBy(item => item.Molde_id))
        {
            if (filaPorMoldeId.TryGetValue(grupo.Key, out var filaMolde))
            {
                moldesPendentes.Add(BuildMoldeRececaoOption(filaMolde));
                continue;
            }

            var molde = await _moldesService.GetByIdAsync(grupo.Key);
            if (molde is null)
                continue;

            moldesPendentes.Add(new MoldeRececaoOption
            {
                MoldeId = molde.MoldeId,
                NumeroMolde = molde.Numero,
                NumeroEncomendaCliente = string.Empty,
                DataEntregaPrevista = DateTime.MinValue,
                Prioridade = int.MaxValue
            });
        }

        return moldesPendentes;
    }

    private async Task<List<PedidoMaterialDto>> GetAllPedidosMaterialAsync()
    {
        var primeiraPagina = await _pedidosMaterialService.GetAllAsync(1, 100);
        if (primeiraPagina is null)
            return [];

        var pedidos = primeiraPagina.Items.ToList();
        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _pedidosMaterialService.GetAllAsync(page, 100);
            if (pagina?.Items is null)
                continue;

            pedidos.AddRange(pagina.Items);
        }

        return pedidos;
    }

    private async Task<PecaDto?> GetPecaByIdSafeAsync(int pecaId, SemaphoreSlim semaphore)
    {
        var semaphoreAdquirido = false;

        try
        {
            await semaphore.WaitAsync();
            semaphoreAdquirido = true;
            return await _pecasService.GetByIdAsync(pecaId);
        }
        catch
        {
            return null;
        }
        finally
        {
            if (semaphoreAdquirido)
                semaphore.Release();
        }
    }

    private async Task<FilaGlobalMoldeItemDto?> VerificarMoldeComPedidoMaterialAtivoAsync(
        FilaGlobalMoldeItemDto candidato,
        SemaphoreSlim semaphore)
    {
        var semaphoreAdquirido = false;

        try
        {
            await semaphore.WaitAsync();
            semaphoreAdquirido = true;

            var pagina = await _pecasService.GetByMoldeIdPendingMaterialReceiptAsync(candidato.MoldeId, 1, 1);
            return pagina?.TotalItems > 0
                ? candidato
                : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (semaphoreAdquirido)
                semaphore.Release();
        }
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

    private static MoldeRececaoOption BuildMoldeRececaoOption(FilaGlobalMoldeItemDto item)
    {
        return new MoldeRececaoOption
        {
            MoldeId = item.MoldeId,
            NumeroMolde = item.NumeroMolde,
            NumeroEncomendaCliente = item.NumeroEncomendaCliente,
            DataEntregaPrevista = item.DataEntregaPrevista,
            Prioridade = item.Prioridade
        };
    }
}
