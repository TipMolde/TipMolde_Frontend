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
            throw new InvalidOperationException($"Nao foi possivel fazer a listagem de utilizadores. Estado: {(int)response.StatusCode}");

        return await response.Content.ReadFromJsonAsync<PagedResult<UtilizadorDto>>();
    }

    public async Task<PagedResult<UtilizadorDto>?> SearchAsync(string searchTerm, int page, int pageSize)
    {
        using var response = await _httpClient.GetAsync($"api/users/search?searchTerm={Uri.EscapeDataString(searchTerm)}&page={page}&pageSize={pageSize}");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Nao foi possivel pesquisar utilizadores. Estado: {(int)response.StatusCode}");

        return await response.Content.ReadFromJsonAsync<PagedResult<UtilizadorDto>>();
    }

    public async Task<UtilizadorDto> GetUtilizadorByIdAsync(int userId)
    {
        using var response = await _httpClient.GetAsync($"api/users/{userId}");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Nao foi possivel obter o utilizador com ID {userId}. Estado: {(int)response.StatusCode}");

        return await response.Content.ReadFromJsonAsync<UtilizadorDto>()
            ?? throw new InvalidOperationException($"Nao foi possivel obter o utilizador com ID {userId}. A resposta veio vazia.");
    }

    public async Task UpdateUtilizadorRoleAsync(int userId, string newRole)
    {
        var payload = new { role = newRole };
        using var response = await _httpClient.PutAsJsonAsync($"api/users/{userId}/role", payload);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Nao foi possivel atualizar o cargo do utilizador com ID {userId}. Estado: {(int)response.StatusCode}");
    }

    public async Task ResetUtilizadorPasswordAsync(int userId, string newPassword)
    {
        var payload = new { newPassword };
        using var response = await _httpClient.PutAsJsonAsync($"api/users/{userId}/password/reset", payload);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Nao foi possivel repor a password do utilizador com ID {userId}. Estado: {(int)response.StatusCode}");
    }

    public async Task CreateAsync(string nome, string email, string password, string role)
    {
        var payload = new { nome, email, password, role };
        using var response = await _httpClient.PostAsJsonAsync("api/users", payload);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Nao foi possivel criar o utilizador. Estado: {(int)response.StatusCode}");
    }

    public async Task UpdateAsync(int userId, string nome, string email)
    {
        var payload = new { nome, email };
        using var response = await _httpClient.PutAsJsonAsync($"api/users/{userId}", payload);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Nao foi possivel atualizar o utilizador com ID {userId}. Estado: {(int)response.StatusCode}");
    }

    public async Task DeleteUtilizadorAsync(int userId)
    {
        using var response = await _httpClient.DeleteAsync($"api/users/{userId}");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Nao foi possivel eliminar o utilizador com ID {userId}. Estado: {(int)response.StatusCode}");
    }
}
