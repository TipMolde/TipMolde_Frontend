using CommunityToolkit.Mvvm.Input;
using TipMolde.Models;
using TipMolde.View;

namespace TipMolde.ViewModel;

/// <summary>
/// Concentra o carregamento principal e o resumo executivo do dashboard.
/// </summary>
public partial class DashboardViewModel
{
    /// <summary>
    /// Carrega os dados principais do dashboard e recompõe todas as secoes derivadas.
    /// </summary>
    /// <returns>Tarefa assincrona do carregamento global do dashboard.</returns>
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
            var encomendasEmProducao = await encomendasEmProducaoTask;
            var todasEncomendas = await todasEncomendasTask;
            var filaGlobalMoldes = await filaGlobalMoldesTask;

            AtualizarResumoExecutivo(todasEncomendas, filaGlobalMoldes);

            try
            {
                await CarregarHeroAsync(encomendasEmProducao, filaGlobalMoldes);
            }
            catch (Exception ex)
            {
                HeroErrorMessage = ex.Message;
                LimparDashboard();
            }

            _todosMoldesPlanificacao = filaGlobalMoldes?.ToList() ?? [];
            AtualizarPlanificacao();

            try
            {
                await AtualizarMoldesRececaoAsync(filaGlobalMoldes, moldeRececaoSelecionadoId);
            }
            catch (Exception ex)
            {
                ResetRececaoMaterial();
                RececaoMaterialErrorMessage = ex.Message;
            }
        }
        catch (Exception ex)
        {
            HeroErrorMessage = ex.Message;
            LimparDashboard();
            LimparResumoExecutivo();
            LimparPlanificacao();
            ResetRececaoMaterial();
        }
        finally
        {
            IsLoadingHero = false;
        }
    }

    /// <summary>
    /// Abre o detalhe do molde mais proximo de entrega.
    /// </summary>
    /// <returns>Tarefa assincrona da navegacao para o detalhe do molde.</returns>
    [RelayCommand(CanExecute = nameof(HasMoldeEntregaDashboard))]
    private async Task AbrirDashboardMoldeAsync()
    {
        if (EntregaMoldeMaisProxima is null || EntregaMoldeMaisProxima.Molde_id <= 0)
            return;

        await _navigationService.GoToAsync($"{nameof(MoldeDetalhePage)}?molde_id={EntregaMoldeMaisProxima.Molde_id}");
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
            HeroErrorMessage = "Não existem encomendas em produção para apresentar no dashboard.";
            LimparDashboard();
            return;
        }

        var melhorCandidato = EncontrarMoldeMaisProximo(encomendasEmProducao, filaGlobalMoldes);
        if (melhorCandidato is null)
        {
            HeroErrorMessage = "Não foi encontrada uma data de entrega válida para os moldes em produção.";
            LimparDashboard();
            return;
        }

        var moldeTask = _moldesService.GetByIdAsync(melhorCandidato.MoldeFila.MoldeId);
        var dashboardTask = _moldesService.GetDashboardCicloVidaAsync(melhorCandidato.MoldeFila.MoldeId);

        await Task.WhenAll(moldeTask, dashboardTask);
        var molde = await moldeTask;
        var dashboard = await dashboardTask;

        if (molde is null && dashboard is null)
        {
            HeroErrorMessage = "Nao foi possivel carregar o resumo do molde mais proximo de entrega.";
            LimparDashboard();
            return;
        }

        if (dashboard is null)
            HeroErrorMessage = "Nao foi possivel carregar o resumo completo do molde mais proximo de entrega.";

        MoldeMaisProximo = molde ?? BuildFallbackMolde(melhorCandidato.MoldeFila);
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
        DashboardMaisProximo = dashboard ?? BuildFallbackDashboard(melhorCandidato.MoldeFila);
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

    private static MoldeDto BuildFallbackMolde(FilaGlobalMoldeItemDto moldeFila)
    {
        return new MoldeDto
        {
            MoldeId = moldeFila.MoldeId,
            Numero = moldeFila.NumeroMolde,
            Nome = moldeFila.NumeroMolde
        };
    }

    private static MoldeCicloVidaDashboardDto BuildFallbackDashboard(FilaGlobalMoldeItemDto moldeFila)
    {
        return new MoldeCicloVidaDashboardDto
        {
            MoldeId = moldeFila.MoldeId,
            NumeroMolde = moldeFila.NumeroMolde,
            TotalPecas = 0,
            Maquinacao = 0,
            Erosao = 0,
            Montagem = 0,
            EmEspera = 0,
            EmTrabalho = 0,
            Concluidas = 0,
            MaterialPendente = 0,
            PercentagemConclusao = 0
        };
    }

    private sealed record MoldeEntregaCandidato(EncomendaResumoDto Encomenda, FilaGlobalMoldeItemDto MoldeFila);
}
