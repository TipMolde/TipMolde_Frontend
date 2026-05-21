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
            throw new InvalidOperationException($"Não foi possível fazer a listagem de utilizadores. Estado: {(int)response.StatusCode}");

        return await response.Content.ReadFromJsonAsync<PagedResult<UtilizadorDto>>();
    }

    public async Task<PagedResult<UtilizadorDto>?> SearchAsync(string searchTerm, int page, int pageSize)
    {
        using var response = await _httpClient.GetAsync($"api/users/search?searchTerm={Uri.EscapeDataString(searchTerm)}&page={page}&pageSize={pageSize}");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Não foi possível pesquisar utilizadores. Estado: {(int)response.StatusCode}");
        return await response.Content.ReadFromJsonAsync<PagedResult<UtilizadorDto>>();
    }

    public async Task<UtilizadorDto> GetUtilizadorByIdAsync(int id)
    {
        using var response = await _httpClient.GetAsync($"api/users/{id}");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Não foi possível obter o utilizador com ID {id}. Estado: {(int)response.StatusCode}");
        return await response.Content.ReadFromJsonAsync<UtilizadorDto>() 
            ?? throw new InvalidOperationException($"Não foi possível obter o utilizador com ID {id}. A resposta veio vazia.");
    }

    public async Task UpdateUtilizadorRoleAsync(int id, string newRole)
    {
        var payload = new { role = newRole };
        using var response = await _httpClient.PutAsJsonAsync($"api/users/{id}/role", payload);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Não foi possível atualizar o cargo do utilizador com ID {id}. Estado: {(int)response.StatusCode}");
    }

    public async Task ResetUtilizadorPasswordAsync(int id)
    {
            using var response = await _httpClient.PostAsync($"api/users/{id}/reset-password", null);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Não foi possível repor a password do utilizador com ID {id}. Estado: {(int)response.StatusCode}");
    }

    public async Task CreateAsync(string nome, string email, string role)
    {
        var payload = new { nome, email, role };
        using var response = await _httpClient.PostAsJsonAsync("api/users", payload);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Não foi possível criar o utilizador. Estado: {(int)response.StatusCode}");
    }

    public async Task UpdateAsync(int id, string nome, string email)
    {
        var payload = new { nome, email };
        using var response = await _httpClient.PutAsJsonAsync($"api/users/{id}", payload);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Não foi possível atualizar o utilizador com ID {id}. Estado: {(int)response.StatusCode}");
    }
    public async Task DeleteUtilizadorAsync(int id)
    {
        using var response = await _httpClient.DeleteAsync($"api/users/{id}");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Não foi possível eliminar o utilizador com ID {id}. Estado: {(int)response.StatusCode}");
    }
}