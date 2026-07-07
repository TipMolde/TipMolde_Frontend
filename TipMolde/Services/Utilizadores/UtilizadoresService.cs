using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Encapsula os pedidos HTTP da feature de utilizadores no frontend.
/// </summary>
public sealed class UtilizadoresService : ApiServiceBase
{
    /// <summary>
    /// Construtor do servico de utilizadores.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com o endpoint base da API.</param>
    public UtilizadoresService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    /// <summary>
    /// Lista utilizadores de forma paginada.
    /// </summary>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com utilizadores.</returns>
    public async Task<PagedResult<UtilizadorDto>?> GetUtilizadoresAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/users?page={page}&pageSize={pageSize}");
        await EnsureSuccessAsync(response, $"Nao foi possivel fazer a listagem de utilizadores. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<PagedResult<UtilizadorDto>>(response);
    }

    /// <summary>
    /// Pesquisa utilizadores por termo livre.
    /// </summary>
    /// <param name="searchTerm">Termo parcial aplicado a pesquisa.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com utilizadores encontrados.</returns>
    public async Task<PagedResult<UtilizadorDto>?> SearchAsync(string searchTerm, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/users/search?searchTerm={Uri.EscapeDataString(searchTerm)}&page={page}&pageSize={pageSize}");
        await EnsureSuccessAsync(response, $"Nao foi possivel pesquisar utilizadores. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<PagedResult<UtilizadorDto>>(response);
    }

    /// <summary>
    /// Obtem um utilizador pelo identificador.
    /// </summary>
    /// <param name="userId">Identificador do utilizador.</param>
    /// <returns>DTO do utilizador pedido.</returns>
    public async Task<UtilizadorDto> GetUtilizadorByIdAsync(int userId)
    {
        using var response = await HttpClient.GetAsync($"api/users/{userId}");
        await EnsureSuccessAsync(response, $"Nao foi possivel obter o utilizador com ID {userId}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<UtilizadorDto>(response)
            ?? throw new InvalidOperationException($"Nao foi possivel obter o utilizador com ID {userId}. A resposta veio vazia.");
    }

    /// <summary>
    /// Obtem o utilizador atualmente autenticado.
    /// </summary>
    /// <returns>DTO do utilizador autenticado.</returns>
    public async Task<UtilizadorDto> GetCurrentUserAsync()
    {
        using var response = await HttpClient.GetAsync("api/users/me");
        await EnsureSuccessAsync(response, $"Nao foi possivel obter o utilizador autenticado. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<UtilizadorDto>(response)
            ?? throw new InvalidOperationException("Nao foi possivel obter o utilizador autenticado. A resposta veio vazia.");
    }

    /// <summary>
    /// Atualiza a role de um utilizador.
    /// </summary>
    /// <param name="userId">Identificador do utilizador alvo.</param>
    /// <param name="newRole">Nova role a aplicar.</param>
    /// <returns>Tarefa assincrona da atualizacao.</returns>
    public async Task UpdateUtilizadorRoleAsync(int userId, string newRole)
    {
        var payload = new { role = newRole };
        using var response = await HttpClient.PutAsJsonAsync($"api/users/{userId}/role", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar o cargo do utilizador com ID {userId}. Estado: {(int)response.StatusCode}");
    }

    /// <summary>
    /// Repoe a password de um utilizador.
    /// </summary>
    /// <param name="userId">Identificador do utilizador alvo.</param>
    /// <param name="newPassword">Nova password a aplicar.</param>
    /// <returns>Tarefa assincrona da operacao.</returns>
    public async Task ResetUtilizadorPasswordAsync(int userId, string newPassword)
    {
        var payload = new { newPassword };
        using var response = await HttpClient.PutAsJsonAsync($"api/users/{userId}/password/reset", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel repor a password do utilizador com ID {userId}. Estado: {(int)response.StatusCode}");
    }

    /// <summary>
    /// Altera a password do utilizador autenticado.
    /// </summary>
    /// <param name="currentPassword">Password atual do utilizador.</param>
    /// <param name="newPassword">Nova password pretendida.</param>
    /// <returns>Tarefa assincrona da operacao.</returns>
    public async Task ChangeCurrentPasswordAsync(string currentPassword, string newPassword)
    {
        var payload = new { currentPassword, newPassword };
        using var response = await HttpClient.PutAsJsonAsync("api/users/me/password", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel alterar a password. Estado: {(int)response.StatusCode}");
    }

    /// <summary>
    /// Cria um novo utilizador.
    /// </summary>
    /// <param name="nome">Nome do utilizador.</param>
    /// <param name="email">Email do utilizador.</param>
    /// <param name="password">Password inicial.</param>
    /// <param name="role">Role funcional atribuida.</param>
    /// <returns>Tarefa assincrona da criacao.</returns>
    public async Task CreateAsync(string nome, string email, string password, string role)
    {
        var payload = new { nome, email, password, role };
        using var response = await HttpClient.PostAsJsonAsync("api/users", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel criar o utilizador. Estado: {(int)response.StatusCode}");
    }

    /// <summary>
    /// Atualiza os dados basicos de um utilizador.
    /// </summary>
    /// <param name="userId">Identificador do utilizador a atualizar.</param>
    /// <param name="nome">Novo nome do utilizador.</param>
    /// <param name="email">Novo email do utilizador.</param>
    /// <returns>Tarefa assincrona da atualizacao.</returns>
    public async Task UpdateAsync(int userId, string nome, string email)
    {
        var payload = new { nome, email };
        using var response = await HttpClient.PutAsJsonAsync($"api/users/{userId}", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar o utilizador com ID {userId}. Estado: {(int)response.StatusCode}");
    }

    /// <summary>
    /// Remove um utilizador existente.
    /// </summary>
    /// <param name="userId">Identificador do utilizador a remover.</param>
    /// <returns>Tarefa assincrona da remocao.</returns>
    public async Task DeleteUtilizadorAsync(int userId)
    {
        using var response = await HttpClient.DeleteAsync($"api/users/{userId}");
        await EnsureSuccessAsync(response, $"Nao foi possivel eliminar o utilizador com ID {userId}. Estado: {(int)response.StatusCode}");
    }
}
