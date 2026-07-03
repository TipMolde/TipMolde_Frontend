using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class RegistosTempoProjetoService : ApiServiceBase
{
    public RegistosTempoProjetoService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<PagedResult<RegistoTempoProjetoDto>?> GetHistoricoAsync(int projetoId, int autorId, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/registos-tempo-projeto?projetoId={projetoId}&autorId={autorId}&page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<RegistoTempoProjetoDto>>(response);
    }

    public async Task<RegistoTempoProjetoDto?> CreateAsync(int projetoId, int autorId, string estadoTempo)
    {
        var payload = new
        {
            Estado_tempo = estadoTempo,
            Projeto_id = projetoId,
            Autor_id = autorId
        };

        using var response = await HttpClient.PostAsJsonAsync("api/registos-tempo-projeto", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel registar o tempo para o projeto {projetoId}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<RegistoTempoProjetoDto>(response);
    }
}
