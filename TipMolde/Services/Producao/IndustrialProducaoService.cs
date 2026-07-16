using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Encapsula as operacoes HTTP do fluxo industrial de producao.
/// </summary>
public sealed class IndustrialProducaoService : ApiServiceBase
{
    /// <summary>
    /// Construtor do servico industrial de producao.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com o endpoint base da API.</param>
    public IndustrialProducaoService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    /// <summary>
    /// Lista eventos industriais pendentes de contexto funcional.
    /// </summary>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com eventos pendentes ou nulo quando a API nao devolve sucesso.</returns>
    public async Task<PagedResult<IndustrialEventoDto>?> GetEventosPendentesAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/industrial/eventos/pendentes?page={page}&pageSize={pageSize}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar eventos industriais pendentes.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<IndustrialEventoDto>>(response);
    }

    /// <summary>
    /// Obtem o evento pendente mais relevante para a maquina atual.
    /// </summary>
    /// <param name="maquinaId">Identificador da maquina.</param>
    /// <returns>Evento pendente ou nulo quando nao existe acao manual.</returns>
    public async Task<IndustrialEventoDto?> GetEventoPendenteMaquinaAsync(int maquinaId)
    {
        using var response = await HttpClient.GetAsync($"api/industrial/maquinas/{maquinaId}/evento-pendente");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar o evento pendente desta maquina.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<IndustrialEventoDto>(response);
    }

    /// <summary>
    /// Obtem a sessao industrial ativa de uma maquina.
    /// </summary>
    /// <param name="maquinaId">Identificador da maquina.</param>
    /// <returns>Resumo da sessao ativa ou nulo quando a maquina nao tem contexto aberto.</returns>
    public async Task<IndustrialSessaoAtivaDto?> GetSessaoAtivaAsync(int maquinaId)
    {
        using var response = await HttpClient.GetAsync($"api/industrial/maquinas/{maquinaId}/sessao-ativa");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar a sessao industrial ativa.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<IndustrialSessaoAtivaDto>(response);
    }

    /// <summary>
    /// Completa manualmente o contexto de um evento industrial pendente.
    /// </summary>
    /// <param name="eventoId">Identificador do evento industrial.</param>
    /// <param name="gestorProducaoId">Operador que assume o contexto do evento.</param>
    /// <param name="pecaId">Peca associada ao evento.</param>
    /// <param name="faseId">Fase produtiva associada ao evento.</param>
    /// <returns>Tarefa assincrona da operacao de contexto.</returns>
    public async Task CompletarContextoAsync(int eventoId, int gestorProducaoId, int pecaId, int faseId)
    {
        var payload = new
        {
            Operador_id = gestorProducaoId,
            Peca_id = pecaId,
            Fase_id = faseId
        };

        using var response = await HttpClient.PostAsJsonAsync($"api/industrial/eventos/{eventoId}/completar-contexto", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel completar o contexto do evento industrial {eventoId}. Estado: {(int)response.StatusCode}");
    }

    /// <summary>
    /// Confirma se um evento STOPPED corresponde a pausa ou conclusao do trabalho.
    /// </summary>
    /// <param name="eventoId">Identificador do evento STOPPED pendente.</param>
    /// <param name="trabalhoConcluido">True quando o trabalho terminou; false quando apenas pausou.</param>
    /// <returns>Tarefa assincrona da confirmacao.</returns>
    public async Task ConfirmarParagemAsync(int eventoId, bool trabalhoConcluido, int? proximaFaseId = null)
    {
        var payload = new
        {
            TrabalhoConcluido = trabalhoConcluido,
            ProximaFase_id = trabalhoConcluido ? proximaFaseId : null
        };

        using var response = await HttpClient.PostAsJsonAsync($"api/industrial/eventos/{eventoId}/confirmar-paragem", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel confirmar a paragem do evento industrial {eventoId}. Estado: {(int)response.StatusCode}");
    }
}
