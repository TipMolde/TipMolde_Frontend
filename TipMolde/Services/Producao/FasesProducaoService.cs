using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Encapsula os pedidos HTTP da feature de fases de producao.
/// </summary>
public sealed class FasesProducaoService : ApiServiceBase
{
    /// <summary>
    /// Construtor do servico de fases de producao.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com o endpoint base da API.</param>
    public FasesProducaoService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    /// <summary>
    /// Lista fases de producao de forma paginada.
    /// </summary>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com fases de producao ou nulo quando a API nao devolve sucesso.</returns>
    public async Task<PagedResult<FaseProducaoItem>?> GetAllAsync(int page, int pageSize)
    {
        try
        {
            using var response = await HttpClient.GetAsync($"api/fases-producao?page={page}&pageSize={pageSize}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await DeserializeAsync<PagedResult<FaseProducaoItem>>(response);
        }
        catch (Exception ex) when (IsConnectivityException(ex))
        {
            throw CreateConnectivityException(ex, "Nao foi possivel contactar o backend para carregar as fases de producao.");
        }
    }

    /// <summary>
    /// Cria uma nova fase de producao.
    /// </summary>
    /// <param name="nome">Nome funcional da fase.</param>
    /// <param name="descricao">Descricao opcional da fase.</param>
    /// <returns>DTO da fase criada.</returns>
    public async Task<FaseProducaoItem?> CreateAsync(string nome, string? descricao)
    {
        var payload = new
        {
            Nome = nome,
            Descricao = descricao
        };

        using var response = await HttpClient.PostAsJsonAsync("api/fases-producao", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel criar a fase de producao {nome}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<FaseProducaoItem>(response);
    }

    /// <summary>
    /// Remove uma fase de producao existente.
    /// </summary>
    /// <param name="faseId">Identificador da fase a remover.</param>
    /// <returns>Tarefa assincrona da remocao.</returns>
    public async Task DeleteAsync(int faseId)
    {
        using var response = await HttpClient.DeleteAsync($"api/fases-producao/{faseId}");
        await EnsureSuccessAsync(response, $"Nao foi possivel eliminar a fase de producao {faseId}. Estado: {(int)response.StatusCode}");
    }
}
