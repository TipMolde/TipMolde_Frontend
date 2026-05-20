using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class UtilizadoresService
{
    private readonly HttpClient _httpClient;

    public UtilizadoresService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<UtilizadorDto>?> GetUtilizadoresAsync(int page, int pageSize)
    {
        using var response = await _httpClient.GetAsync($"api/users?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"A API devolveu o estado {(int)response.StatusCode} ao pedir utilizadores.");

        return await response.Content.ReadFromJsonAsync<PagedResult<UtilizadorDto>>();
    }
}