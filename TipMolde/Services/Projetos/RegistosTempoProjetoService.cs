using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Encapsula os pedidos HTTP de registo temporal em projetos.
/// </summary>
public sealed class RegistosTempoProjetoService : ApiServiceBase
{
    /// <summary>
    /// Construtor do servico de registos de tempo de projeto.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com o endpoint base da API.</param>
    public RegistosTempoProjetoService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    /// <summary>
    /// Lista o historico temporal de um projeto para um autor.
    /// </summary>
    /// <param name="projetoId">Identificador do projeto.</param>
    /// <param name="autorId">Identificador do autor.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com historico temporal ou nulo quando a API nao devolve sucesso.</returns>
    public async Task<PagedResult<RegistoTempoProjetoDto>?> GetHistoricoAsync(int projetoId, int autorId, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/registos-tempo-projeto?projetoId={projetoId}&autorId={autorId}&page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<RegistoTempoProjetoDto>>(response);
    }

    /// <summary>
    /// Cria um novo registo temporal para um projeto.
    /// </summary>
    /// <param name="projetoId">Identificador do projeto.</param>
    /// <param name="autorId">Identificador do autor do registo.</param>
    /// <param name="estadoTempo">Estado temporal a registar.</param>
    /// <returns>DTO do registo criado.</returns>
    public async Task<RegistoTempoProjetoDto?> CreateAsync(int projetoId, int autorId, string estadoTempo)
    {
        var payload = new
        {
            Estado_tempo = estadoTempo,
            Projeto_id = projetoId,
            Autor_id = autorId
        };

        using var response = await HttpClient.PostAsJsonAsync("api/registos-tempo-projeto", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel registar o tempo para o projeto {projetoId}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<RegistoTempoProjetoDto>(response);
    }
}
