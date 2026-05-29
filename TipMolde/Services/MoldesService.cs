using System.Text.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class MoldesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public MoldesService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MoldeDto?> GetByIdAsync(int moldeId)
    {
        using var response = await _httpClient.GetAsync($"api/moldes/{moldeId}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<MoldeDto>(response);
    }

    public async Task<PagedResult<MoldeDto>?> GetAllAsync(int page, int pageSize)
    {
        using var response = await _httpClient.GetAsync($"api/moldes?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<MoldeDto>>(response);
    }

    public async Task<PagedResult<MoldeDto>?> GetByEncomendaIdAsync(int encomendaId, int page, int pageSize)
    {
        using var response = await _httpClient.GetAsync(
            $"api/moldes/por-encomenda/{encomendaId}?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<MoldeDto>>(response);
    }

    public async Task<MoldeCicloVidaDashboardDto?> GetDashboardCicloVidaAsync(int moldeId)
    {
        using var response = await _httpClient.GetAsync($"api/moldes/{moldeId}/dashboard-ciclo-vida");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<MoldeCicloVidaDashboardDto>(response);
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }
}
