using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class ClientesService : ApiServiceBase
{
    public ClientesService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<PagedResult<ClienteDto>?> GetClientesAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/clientes?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<ClienteDto>>(response);
    }

    public async Task<PagedResult<ClienteDto>?> SearchByNameAsync(string searchTerm, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/clientes/search/by-name?searchTerm={Uri.EscapeDataString(searchTerm)}&page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<ClienteDto>>(response);
    }

    public async Task<PagedResult<ClienteDto>?> SearchBySiglaAsync(string searchTerm, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/clientes/search/by-sigla?searchTerm={Uri.EscapeDataString(searchTerm)}&page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<ClienteDto>>(response);
    }

    public async Task<ClienteComEncomendasDto?> GetClienteWithEncomendasAsync(int clienteId)
    {
        using var response = await HttpClient.GetAsync($"api/clientes/{clienteId}/encomendas");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<ClienteComEncomendasDto>(response);
    }

    public async Task<ClienteDto?> GetByIdAsync(int clienteId)
    {
        using var response = await HttpClient.GetAsync($"api/clientes/{clienteId}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<ClienteDto>(response);
    }

    public async Task<ClienteDto?> CreateAsync(
        string nome,
        string nif,
        string sigla,
        string? pais,
        string? email,
        string? telefone)
    {
        var payload = new
        {
            nome,
            nif,
            sigla,
            pais,
            email,
            telefone
        };

        using var response = await HttpClient.PostAsJsonAsync("api/clientes", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel criar o cliente. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<ClienteDto>(response);
    }

    public async Task UpdateAsync(
        int clienteId,
        string nome,
        string nif,
        string sigla,
        string? pais,
        string? email,
        string? telefone)
    {
        var payload = new
        {
            nome,
            nif,
            sigla,
            pais,
            email,
            telefone
        };

        using var response = await HttpClient.PutAsJsonAsync($"api/clientes/{clienteId}", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar o cliente com ID {clienteId}. Estado: {(int)response.StatusCode}");
    }

    public async Task DeleteAsync(int clienteId)
    {
        using var response = await HttpClient.DeleteAsync($"api/clientes/{clienteId}");
        await EnsureSuccessAsync(response, $"Nao foi possivel eliminar o cliente com ID {clienteId}. Estado: {(int)response.StatusCode}");
    }
}
