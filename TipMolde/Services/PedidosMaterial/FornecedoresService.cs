using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Encapsula os pedidos HTTP da feature de fornecedores no frontend.
/// </summary>
public sealed class FornecedoresService : ApiServiceBase
{
    /// <summary>
    /// Construtor do servico de fornecedores.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com o endpoint base da API.</param>
    public FornecedoresService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    /// <summary>
    /// Lista fornecedores de forma paginada.
    /// </summary>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com fornecedores ou nulo quando a API nao devolve sucesso.</returns>
    public async Task<PagedResult<FornecedorDto>?> GetAllAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/fornecedores?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<FornecedorDto>>(response);
    }

    /// <summary>
    /// Obtem um fornecedor pelo identificador.
    /// </summary>
    /// <param name="fornecedorId">Identificador do fornecedor.</param>
    /// <returns>DTO do fornecedor ou nulo quando nao e encontrado.</returns>
    public async Task<FornecedorDto?> GetByIdAsync(int fornecedorId)
    {
        using var response = await HttpClient.GetAsync($"api/fornecedores/{fornecedorId}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<FornecedorDto>(response);
    }

    /// <summary>
    /// Cria um novo fornecedor.
    /// </summary>
    /// <param name="nome">Nome do fornecedor.</param>
    /// <param name="nif">NIF do fornecedor.</param>
    /// <param name="morada">Morada principal.</param>
    /// <param name="email">Email principal.</param>
    /// <param name="telefone">Telefone principal.</param>
    /// <returns>DTO do fornecedor criado.</returns>
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

    /// <summary>
    /// Atualiza os dados de um fornecedor existente.
    /// </summary>
    /// <param name="fornecedorId">Identificador do fornecedor a atualizar.</param>
    /// <param name="nome">Novo nome do fornecedor.</param>
    /// <param name="nif">Novo NIF do fornecedor.</param>
    /// <param name="morada">Nova morada.</param>
    /// <param name="email">Novo email.</param>
    /// <param name="telefone">Novo telefone.</param>
    /// <returns>Tarefa assincrona da atualizacao.</returns>
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

    /// <summary>
    /// Remove um fornecedor existente.
    /// </summary>
    /// <param name="fornecedorId">Identificador do fornecedor a remover.</param>
    /// <returns>Tarefa assincrona da remocao.</returns>
    public async Task DeleteAsync(int fornecedorId)
    {
        using var response = await HttpClient.DeleteAsync($"api/fornecedores/{fornecedorId}");
        await EnsureSuccessAsync(response, $"Nao foi possivel eliminar o fornecedor com ID {fornecedorId}. Estado: {(int)response.StatusCode}");
    }
}
