using System.Net.Http.Json;
using System.Text.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class EncomendasService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public EncomendasService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<EncomendaResumoDto>?> GetEncomendasNaoConcluidasAsync(int page, int pageSize)
    {
        using var response = await _httpClient.GetAsync(
            $"api/encomendas/em-producao?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<EncomendaResumoDto>>(response);
    }

    public async Task<EncomendaResumoDto?> GetByIdAsync(int encomendaId)
    {
        using var response = await _httpClient.GetAsync($"api/encomendas/{encomendaId}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<EncomendaResumoDto>(response);
    }

    public async Task<PagedResult<EncomendaMoldeDto>?> GetEncomendaMoldesByEncomendaIdAsync(int encomendaId, int page, int pageSize)
    {
        using var response = await _httpClient.GetAsync(
            $"api/encomenda-moldes/por-encomenda/{encomendaId}?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<EncomendaMoldeDto>>(response);
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }
}
