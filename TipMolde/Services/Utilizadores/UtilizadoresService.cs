using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class UtilizadoresService : ApiServiceBase
{
    public UtilizadoresService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<PagedResult<UtilizadorDto>?> GetUtilizadoresAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/users?page={page}&pageSize={pageSize}");
        await EnsureSuccessAsync(response, $"Nao foi possivel fazer a listagem de utilizadores. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<PagedResult<UtilizadorDto>>(response);
    }

    public async Task<PagedResult<UtilizadorDto>?> SearchAsync(string searchTerm, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/users/search?searchTerm={Uri.EscapeDataString(searchTerm)}&page={page}&pageSize={pageSize}");
        await EnsureSuccessAsync(response, $"Nao foi possivel pesquisar utilizadores. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<PagedResult<UtilizadorDto>>(response);
    }

    public async Task<UtilizadorDto> GetUtilizadorByIdAsync(int userId)
    {
        using var response = await HttpClient.GetAsync($"api/users/{userId}");
        await EnsureSuccessAsync(response, $"Nao foi possivel obter o utilizador com ID {userId}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<UtilizadorDto>(response)
            ?? throw new InvalidOperationException($"Nao foi possivel obter o utilizador com ID {userId}. A resposta veio vazia.");
    }

    public async Task<UtilizadorDto> GetCurrentUserAsync()
    {
        using var response = await HttpClient.GetAsync("api/users/me");
        await EnsureSuccessAsync(response, $"Nao foi possivel obter o utilizador autenticado. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<UtilizadorDto>(response)
            ?? throw new InvalidOperationException("Nao foi possivel obter o utilizador autenticado. A resposta veio vazia.");
    }

    public async Task UpdateUtilizadorRoleAsync(int userId, string newRole)
    {
        var payload = new { role = newRole };
        using var response = await HttpClient.PutAsJsonAsync($"api/users/{userId}/role", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar o cargo do utilizador com ID {userId}. Estado: {(int)response.StatusCode}");
    }

    public async Task ResetUtilizadorPasswordAsync(int userId, string newPassword)
    {
        var payload = new { newPassword };
        using var response = await HttpClient.PutAsJsonAsync($"api/users/{userId}/password/reset", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel repor a password do utilizador com ID {userId}. Estado: {(int)response.StatusCode}");
    }

    public async Task ChangeCurrentPasswordAsync(string currentPassword, string newPassword)
    {
        var payload = new { currentPassword, newPassword };
        using var response = await HttpClient.PutAsJsonAsync("api/users/me/password", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel alterar a password. Estado: {(int)response.StatusCode}");
    }

    public async Task CreateAsync(string nome, string email, string password, string role)
    {
        var payload = new { nome, email, password, role };
        using var response = await HttpClient.PostAsJsonAsync("api/users", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel criar o utilizador. Estado: {(int)response.StatusCode}");
    }

    public async Task UpdateAsync(int userId, string nome, string email)
    {
        var payload = new { nome, email };
        using var response = await HttpClient.PutAsJsonAsync($"api/users/{userId}", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar o utilizador com ID {userId}. Estado: {(int)response.StatusCode}");
    }

    public async Task DeleteUtilizadorAsync(int userId)
    {
        using var response = await HttpClient.DeleteAsync($"api/users/{userId}");
        await EnsureSuccessAsync(response, $"Nao foi possivel eliminar o utilizador com ID {userId}. Estado: {(int)response.StatusCode}");
    }
}
