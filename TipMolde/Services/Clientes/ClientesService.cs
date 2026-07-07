using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Encapsula os pedidos HTTP da feature de clientes no frontend.
/// </summary>
/// <remarks>
/// Fornece operacoes de listagem, pesquisa, detalhe e manutencao
/// do catalogo de clientes consumido pelos view models.
/// </remarks>
public sealed class ClientesService : ApiServiceBase
{
    /// <summary>
    /// Construtor do servico de clientes.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com o endpoint base da API.</param>
    public ClientesService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    /// <summary>
    /// Lista clientes de forma paginada.
    /// </summary>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com clientes ou nulo quando a API falha sem detalhe tratavel.</returns>
    public async Task<PagedResult<ClienteDto>?> GetClientesAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/clientes?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<ClienteDto>>(response);
    }

    /// <summary>
    /// Pesquisa clientes por nome.
    /// </summary>
    /// <param name="searchTerm">Termo parcial aplicado ao nome do cliente.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com clientes correspondentes ou nulo quando a API falha.</returns>
    public async Task<PagedResult<ClienteDto>?> SearchByNameAsync(string searchTerm, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/clientes/search/by-name?searchTerm={Uri.EscapeDataString(searchTerm)}&page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<ClienteDto>>(response);
    }

    /// <summary>
    /// Pesquisa clientes por sigla.
    /// </summary>
    /// <param name="searchTerm">Termo parcial aplicado a sigla do cliente.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com clientes correspondentes ou nulo quando a API falha.</returns>
    public async Task<PagedResult<ClienteDto>?> SearchBySiglaAsync(string searchTerm, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/clientes/search/by-sigla?searchTerm={Uri.EscapeDataString(searchTerm)}&page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<ClienteDto>>(response);
    }

    /// <summary>
    /// Obtem um cliente com as encomendas associadas.
    /// </summary>
    /// <param name="clienteId">Identificador do cliente.</param>
    /// <returns>DTO do cliente enriquecido com encomendas ou nulo quando nao existe.</returns>
    public async Task<ClienteComEncomendasDto?> GetClienteWithEncomendasAsync(int clienteId)
    {
        using var response = await HttpClient.GetAsync($"api/clientes/{clienteId}/encomendas");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<ClienteComEncomendasDto>(response);
    }

    /// <summary>
    /// Obtem um cliente pelo identificador.
    /// </summary>
    /// <param name="clienteId">Identificador interno do cliente.</param>
    /// <returns>DTO do cliente ou nulo quando nao e encontrado.</returns>
    public async Task<ClienteDto?> GetByIdAsync(int clienteId)
    {
        using var response = await HttpClient.GetAsync($"api/clientes/{clienteId}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<ClienteDto>(response);
    }

    /// <summary>
    /// Cria um novo cliente.
    /// </summary>
    /// <param name="nome">Nome completo do cliente.</param>
    /// <param name="nif">Identificacao fiscal do cliente.</param>
    /// <param name="sigla">Sigla funcional usada no sistema.</param>
    /// <param name="pais">Pais do cliente quando conhecido.</param>
    /// <param name="email">Email principal de contacto.</param>
    /// <param name="telefone">Telefone principal de contacto.</param>
    /// <returns>DTO do cliente criado.</returns>
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

    /// <summary>
    /// Atualiza os dados de um cliente existente.
    /// </summary>
    /// <param name="clienteId">Identificador do cliente a atualizar.</param>
    /// <param name="nome">Novo nome do cliente.</param>
    /// <param name="nif">Novo NIF do cliente.</param>
    /// <param name="sigla">Nova sigla funcional.</param>
    /// <param name="pais">Novo pais do cliente.</param>
    /// <param name="email">Novo email principal.</param>
    /// <param name="telefone">Novo telefone principal.</param>
    /// <returns>Tarefa assincrona da operacao de atualizacao.</returns>
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

    /// <summary>
    /// Remove um cliente existente.
    /// </summary>
    /// <param name="clienteId">Identificador do cliente a remover.</param>
    /// <returns>Tarefa assincrona da remocao.</returns>
    public async Task DeleteAsync(int clienteId)
    {
        using var response = await HttpClient.DeleteAsync($"api/clientes/{clienteId}");
        await EnsureSuccessAsync(response, $"Nao foi possivel eliminar o cliente com ID {clienteId}. Estado: {(int)response.StatusCode}");
    }
}
