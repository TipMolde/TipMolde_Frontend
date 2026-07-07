using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Encapsula os pedidos HTTP da feature de pedidos de material.
/// </summary>
public sealed class PedidosMaterialService : ApiServiceBase
{
    /// <summary>
    /// Construtor do servico de pedidos de material.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com o endpoint base da API.</param>
    public PedidosMaterialService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    /// <summary>
    /// Lista pedidos de material de forma paginada.
    /// </summary>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com pedidos ou nulo quando a API nao devolve sucesso.</returns>
    public async Task<PagedResult<PedidoMaterialDto>?> GetAllAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/pedidos-material?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<PedidoMaterialDto>>(response);
    }

    /// <summary>
    /// Cria um novo pedido de material.
    /// </summary>
    /// <param name="request">Dados funcionais do pedido a criar.</param>
    /// <returns>DTO do pedido criado.</returns>
    public async Task<PedidoMaterialDto?> CreateAsync(CreatePedidoMaterialRequest request)
    {
        using var response = await HttpClient.PostAsJsonAsync("api/pedidos-material", request);
        await EnsureSuccessAsync(response, "Nao foi possivel criar o pedido de material.");

        return await DeserializeAsync<PedidoMaterialDto>(response);
    }

    /// <summary>
    /// Regista a rececao de um pedido de material existente.
    /// </summary>
    /// <param name="pedidoMaterialId">Identificador do pedido recebido.</param>
    /// <returns>Tarefa assincrona da operacao de rececao.</returns>
    public async Task RegistarRececaoAsync(int pedidoMaterialId)
    {
        using var response = await HttpClient.PutAsync($"api/pedidos-material/{pedidoMaterialId}/rececao", null);
        await EnsureSuccessAsync(response, $"Nao foi possivel registar a rececao do pedido {pedidoMaterialId}.");
    }
}
