using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class FornecedoresService : ApiServiceBase
{
    public FornecedoresService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<PagedResult<FornecedorDto>?> GetAllAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/fornecedores?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<FornecedorDto>>(response);
    }
}
