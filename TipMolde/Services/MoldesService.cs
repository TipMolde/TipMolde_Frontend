using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class MoldesService : ApiServiceBase
{
    public MoldesService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<MoldeDto?> GetByIdAsync(int moldeId)
    {
        using var response = await HttpClient.GetAsync($"api/moldes/{moldeId}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<MoldeDto>(response);
    }

    public async Task<PagedResult<MoldeDto>?> GetAllAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/moldes?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<MoldeDto>>(response);
    }

    public async Task<PagedResult<MoldeDto>?> GetByEncomendaIdAsync(int encomendaId, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/moldes/por-encomenda/{encomendaId}?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<MoldeDto>>(response);
    }

    public async Task<MoldeCicloVidaDashboardDto?> GetDashboardCicloVidaAsync(int moldeId)
    {
        using var response = await HttpClient.GetAsync($"api/moldes/{moldeId}/dashboard-ciclo-vida");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<MoldeCicloVidaDashboardDto>(response);
    }

    /// <summary>
    /// Cria um novo molde e faz a associacao inicial a uma encomenda a partir do endpoint existente.
    /// </summary>
    public async Task<MoldeDto?> CreateAsync(
        string numero,
        string? numeroMoldeCliente,
        string nome,
        string? imagemCapaPath,
        string? descricao,
        int numeroCavidades,
        string tipoPedido)
    {
        var payload = new
        {
            Numero = numero,
            NumeroMoldeCliente = numeroMoldeCliente,
            Nome = nome,
            ImagemCapaPath = imagemCapaPath,
            Descricao = descricao,
            Numero_cavidades = numeroCavidades,
            TipoPedido = tipoPedido
        };

        using var response = await HttpClient.PostAsJsonAsync("api/moldes", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel criar o molde {numero}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<MoldeDto>(response);
    }

}
