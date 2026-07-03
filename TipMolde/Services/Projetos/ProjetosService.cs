using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class ProjetosService : ApiServiceBase
{
    public ProjetosService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<PagedResult<ProjetoDto>?> GetByMoldeIdAsync(int moldeId, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/projetos/por-molde/{moldeId}?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<ProjetoDto>>(response);
    }

    public async Task<PagedResult<ProjetoDto>?> GetAllAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/projetos?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<ProjetoDto>>(response);
    }

    public async Task<ProjetoDto?> CreateAsync(CreateProjetoRequest request)
    {
        var payload = new
        {
            NomeProjeto = request.NomeProjeto.Trim(),
            SoftwareUtilizado = request.SoftwareUtilizado.Trim(),
            TipoProjeto = request.TipoProjeto,
            CaminhoPastaServidor = request.CaminhoPastaServidor.Trim(),
            Molde_id = request.MoldeId
        };

        using var response = await HttpClient.PostAsJsonAsync("api/projetos", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel criar o projeto para o molde {request.MoldeId}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<ProjetoDto>(response);
    }

    public async Task<ProjetoComRevisoesDto?> GetWithRevisoesAsync(int projetoId)
    {
        using var response = await HttpClient.GetAsync($"api/projetos/{projetoId}/com-revisoes");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<ProjetoComRevisoesDto>(response);
    }
}

public sealed record CreateProjetoRequest
{
    public string NomeProjeto { get; init; } = string.Empty;
    public string SoftwareUtilizado { get; init; } = string.Empty;
    public string TipoProjeto { get; init; } = string.Empty;
    public string CaminhoPastaServidor { get; init; } = string.Empty;
    public int MoldeId { get; init; }
}
