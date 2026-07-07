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
}
