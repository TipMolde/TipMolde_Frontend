using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class IndustrialProducaoService : ApiServiceBase
{
    public IndustrialProducaoService(HttpClient httpClient)
        : base(httpClient)
    {
    }

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
