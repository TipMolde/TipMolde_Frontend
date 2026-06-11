using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Centraliza o calculo temporario e o rebalanceamento das prioridades globais dos moldes
/// com base nas encomendas ainda em aberto.
/// </summary>
public sealed class GlobalMoldePriorityService
{
    private readonly EncomendasService _encomendasService;

    /// <summary>
    /// Construtor do servico de prioridades globais dos moldes.
    /// </summary>
    public GlobalMoldePriorityService(EncomendasService encomendasService)
    {
        _encomendasService = encomendasService;
    }

    /// <summary>
    /// Calcula prioridades estimadas para moldes ainda nao gravados, misturando-os com os moldes
    /// das encomendas abertas para manter a mesma ordenacao global.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, int>> CalculateDraftPrioritiesAsync(
        IEnumerable<GlobalMoldeDraftPriorityItem> drafts)
    {
        var draftList = drafts
            .Where(item => !string.IsNullOrWhiteSpace(item.DraftKey))
            .ToList();

        if (draftList.Count == 0)
            return new Dictionary<string, int>(StringComparer.Ordinal);

        var existingAssociations = await GetOpenEncomendaMoldesAsync();
        var assignments = BuildAssignments(existingAssociations, draftList);

        return assignments
            .Where(item => !string.IsNullOrWhiteSpace(item.DraftKey))
            .ToDictionary(
                item => item.DraftKey!,
                item => item.Prioridade,
                StringComparer.Ordinal);
    }

    /// <summary>
    /// Reaplica as prioridades globais aos moldes existentes nas encomendas abertas.
    /// </summary>
    public async Task RebalanceAsync()
    {
        var existingAssociations = await GetOpenEncomendaMoldesAsync();
        var assignments = BuildAssignments(existingAssociations, []);

        foreach (var assignment in assignments)
        {
            if (!assignment.EncomendaMoldeId.HasValue)
                continue;

            if (assignment.Prioridade == assignment.CurrentPrioridade)
                continue;

            await _encomendasService.UpdateEncomendaMoldeAsync(
                assignment.EncomendaMoldeId.Value,
                prioridade: assignment.Prioridade);
        }
    }

    /// <summary>
    /// Carrega todos os moldes associados a encomendas nao concluidas nem canceladas.
    /// </summary>
    public async Task<IReadOnlyList<OpenEncomendaMoldePriorityItem>> GetOpenEncomendaMoldesAsync()
    {
        var encomendas = await GetAllOpenEncomendasAsync();
        if (encomendas.Count == 0)
            return [];

        var tarefas = encomendas
            .Select(async encomenda => new
            {
                Encomenda = encomenda,
                Moldes = await GetAllEncomendaMoldesAsync(encomenda.Encomenda_id)
            })
            .ToList();

        var resultados = await Task.WhenAll(tarefas);
        var items = new List<OpenEncomendaMoldePriorityItem>();

        foreach (var resultado in resultados)
        {
            foreach (var molde in resultado.Moldes)
            {
                items.Add(new OpenEncomendaMoldePriorityItem(
                    molde.EncomendaMolde_id,
                    resultado.Encomenda.Encomenda_id,
                    molde.Molde_id,
                    molde.DataEntregaPrevista,
                    molde.Prioridade,
                    resultado.Encomenda.NumeroEncomendaClienteDisplay,
                    molde.NumeroMolde ?? string.Empty));
            }
        }

        return items;
    }

    private async Task<List<EncomendaResumoDto>> GetAllOpenEncomendasAsync()
    {
        var primeiraPagina = await _encomendasService.GetEncomendasNaoConcluidasAsync(1, 100);
        if (primeiraPagina is null)
            return [];

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

    private async Task<List<EncomendaMoldeDto>> GetAllEncomendaMoldesAsync(int encomendaId)
    {
        var primeiraPagina = await _encomendasService.GetEncomendaMoldesByEncomendaIdAsync(encomendaId, 1, 100);
        if (primeiraPagina is null)
            return [];

        var encomendaMoldes = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _encomendasService.GetEncomendaMoldesByEncomendaIdAsync(encomendaId, page, 100);
            if (pagina?.Items is null)
                continue;

            encomendaMoldes.AddRange(pagina.Items);
        }

        return encomendaMoldes;
    }

    // Porque: esta ordenacao e a base comum para preview no frontend e para o rebalanceamento
    // final das prioridades, garantindo um criterio estavel mesmo quando entram novos moldes.
    private static List<PriorityAssignment> BuildAssignments(
        IReadOnlyCollection<OpenEncomendaMoldePriorityItem> existingAssociations,
        IReadOnlyCollection<GlobalMoldeDraftPriorityItem> drafts)
    {
        var candidates = new List<PriorityCandidate>(existingAssociations.Count + drafts.Count);
        var sequence = 0;

        foreach (var item in existingAssociations)
        {
            candidates.Add(new PriorityCandidate(
                item.EncomendaMoldeId,
                DraftKey: null,
                item.EncomendaId,
                item.MoldeId,
                NormalizeDate(item.DataEntregaPrevista),
                item.Prioridade,
                item.NumeroEncomendaCliente,
                item.NumeroMolde,
                sequence++));
        }

        foreach (var item in drafts)
        {
            candidates.Add(new PriorityCandidate(
                EncomendaMoldeId: null,
                item.DraftKey,
                item.EncomendaId,
                item.MoldeId,
                NormalizeDate(item.DataEntregaPrevista),
                CurrentPrioridade: int.MaxValue,
                NumeroEncomendaCliente: string.Empty,
                item.NumeroMolde,
                sequence++));
        }

        var ordered = candidates
            .OrderBy(item => item.DataEntregaPrevista)
            .ThenBy(item => item.CurrentPrioridade <= 0 ? int.MaxValue : item.CurrentPrioridade)
            .ThenBy(item => item.EncomendaId)
            .ThenBy(item => item.NumeroEncomendaCliente, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.NumeroMolde, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.MoldeId)
            .ThenBy(item => item.Sequence)
            .ToList();

        var assignments = new List<PriorityAssignment>(ordered.Count);

        for (var index = 0; index < ordered.Count; index++)
        {
            var item = ordered[index];
            assignments.Add(new PriorityAssignment(
                item.EncomendaMoldeId,
                item.DraftKey,
                index + 1,
                item.CurrentPrioridade));
        }

        return assignments;
    }

    private static DateTime NormalizeDate(DateTime value)
    {
        return value <= DateTime.MinValue
            ? DateTime.MaxValue.Date
            : value.Date;
    }

    private sealed record PriorityCandidate(
        int? EncomendaMoldeId,
        string? DraftKey,
        int EncomendaId,
        int MoldeId,
        DateTime DataEntregaPrevista,
        int CurrentPrioridade,
        string NumeroEncomendaCliente,
        string NumeroMolde,
        int Sequence);

    private sealed record PriorityAssignment(
        int? EncomendaMoldeId,
        string? DraftKey,
        int Prioridade,
        int CurrentPrioridade);
}

/// <summary>
/// Representa um molde ja associado a uma encomenda aberta para efeitos de prioridade global.
/// </summary>
public sealed record OpenEncomendaMoldePriorityItem(
    int EncomendaMoldeId,
    int EncomendaId,
    int MoldeId,
    DateTime DataEntregaPrevista,
    int Prioridade,
    string NumeroEncomendaCliente,
    string NumeroMolde);

/// <summary>
/// Representa um molde ainda em rascunho usado para simular a prioridade antes da gravacao.
/// </summary>
public sealed record GlobalMoldeDraftPriorityItem(
    string DraftKey,
    int EncomendaId,
    int MoldeId,
    DateTime DataEntregaPrevista,
    string NumeroMolde);
