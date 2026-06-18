using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class PedidosMaterialService : ApiServiceBase
{
    public PedidosMaterialService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<PagedResult<PedidoMaterialDto>?> GetAllAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/pedidos-material?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<PedidoMaterialDto>>(response);
    }

    public async Task<PedidoMaterialDto?> CreateAsync(CreatePedidoMaterialRequest request)
    {
        using var response = await HttpClient.PostAsJsonAsync("api/pedidos-material", request);
        await EnsureSuccessAsync(response, "Nao foi possivel criar o pedido de material.");

        return await DeserializeAsync<PedidoMaterialDto>(response);
    }

    public async Task RegistarRececaoAsync(int pedidoMaterialId)
    {
        using var response = await HttpClient.PutAsync($"api/pedidos-material/{pedidoMaterialId}/rececao", null);
        await EnsureSuccessAsync(response, $"Nao foi possivel registar a rececao do pedido {pedidoMaterialId}.");
    }
}
