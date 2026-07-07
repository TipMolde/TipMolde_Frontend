using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Encapsula os pedidos HTTP da feature de projetos.
/// </summary>
public sealed class ProjetosService : ApiServiceBase
{
    /// <summary>
    /// Construtor do servico de projetos.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com o endpoint base da API.</param>
    public ProjetosService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    /// <summary>
    /// Lista projetos associados a um molde.
    /// </summary>
    /// <param name="moldeId">Identificador do molde.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com projetos do molde ou nulo quando a API nao devolve sucesso.</returns>
    public async Task<PagedResult<ProjetoDto>?> GetByMoldeIdAsync(int moldeId, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/projetos/por-molde/{moldeId}?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<ProjetoDto>>(response);
    }

    /// <summary>
    /// Lista todos os projetos de forma paginada.
    /// </summary>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com projetos ou nulo quando a API nao devolve sucesso.</returns>
    public async Task<PagedResult<ProjetoDto>?> GetAllAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/projetos?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<ProjetoDto>>(response);
    }

    /// <summary>
    /// Cria um novo projeto para um molde.
    /// </summary>
    /// <param name="request">Dados funcionais do projeto a criar.</param>
    /// <returns>DTO do projeto criado.</returns>
    public async Task<ProjetoDto?> CreateAsync(CreateProjetoRequest request)
    {
        var payload = new
        {
            NomeProjeto = request.NomeProjeto.Trim(),
            SoftwareUtilizado = request.SoftwareUtilizado.Trim(),
            TipoProjeto = request.TipoProjeto,
            CaminhoPastaServidor = request.CaminhoPastaServidor.Trim(),
            Molde_id = request.MoldeId
        };

        using var response = await HttpClient.PostAsJsonAsync("api/projetos", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel criar o projeto para o molde {request.MoldeId}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<ProjetoDto>(response);
    }

    /// <summary>
    /// Obtem um projeto com o historico de revisoes associado.
    /// </summary>
    /// <param name="projetoId">Identificador do projeto.</param>
    /// <returns>DTO do projeto enriquecido com revisoes ou nulo quando nao existe.</returns>
    public async Task<ProjetoComRevisoesDto?> GetWithRevisoesAsync(int projetoId)
    {
        using var response = await HttpClient.GetAsync($"api/projetos/{projetoId}/com-revisoes");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<ProjetoComRevisoesDto>(response);
    }
}

/// <summary>
/// Representa os dados necessarios para criar um projeto.
/// </summary>
public sealed record CreateProjetoRequest
{
    public string NomeProjeto { get; init; } = string.Empty;
    public string SoftwareUtilizado { get; init; } = string.Empty;
    public string TipoProjeto { get; init; } = string.Empty;
    public string CaminhoPastaServidor { get; init; } = string.Empty;
    public int MoldeId { get; init; }
}
