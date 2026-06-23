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

    public async Task<FornecedorDto?> GetByIdAsync(int fornecedorId)
    {
        using var response = await HttpClient.GetAsync($"api/fornecedores/{fornecedorId}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<FornecedorDto>(response);
    }

    public async Task<FornecedorDto?> CreateAsync(
        string nome,
        string nif,
        string? morada,
        string? email,
        string? telefone)
    {
        var payload = new
        {
            nome,
            nif,
            morada,
            email,
            telefone
        };

        using var response = await HttpClient.PostAsJsonAsync("api/fornecedores", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel criar o fornecedor. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<FornecedorDto>(response);
    }

    public async Task UpdateAsync(
        int fornecedorId,
        string? nome,
        string? nif,
        string? morada,
        string? email,
        string? telefone)
    {
        var payload = new
        {
            nome,
            nif,
            morada,
            email,
            telefone
        };

        using var response = await HttpClient.PutAsJsonAsync($"api/fornecedores/{fornecedorId}", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar o fornecedor com ID {fornecedorId}. Estado: {(int)response.StatusCode}");
    }

    public async Task DeleteAsync(int fornecedorId)
    {
        using var response = await HttpClient.DeleteAsync($"api/fornecedores/{fornecedorId}");
        await EnsureSuccessAsync(response, $"Nao foi possivel eliminar o fornecedor com ID {fornecedorId}. Estado: {(int)response.StatusCode}");
    }
}
