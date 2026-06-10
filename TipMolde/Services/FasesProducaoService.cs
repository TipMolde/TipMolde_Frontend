using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class FasesProducaoService : ApiServiceBase
{
    public FasesProducaoService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<PagedResult<FaseProducaoItem>?> GetAllAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/fases-producao?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<FaseProducaoItem>>(response);
    }

    public async Task<FaseProducaoItem?> CreateAsync(string nome, string? descricao)
    {
        var payload = new
        {
            Nome = nome,
            Descricao = descricao
        };

        using var response = await HttpClient.PostAsJsonAsync("api/fases-producao", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel criar a fase de producao {nome}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<FaseProducaoItem>(response);
    }

    public async Task DeleteAsync(int faseId)
    {
        using var response = await HttpClient.DeleteAsync($"api/fases-producao/{faseId}");
        await EnsureSuccessAsync(response, $"Nao foi possivel eliminar a fase de producao {faseId}. Estado: {(int)response.StatusCode}");
    }
}
