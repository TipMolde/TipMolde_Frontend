using System.Net;
using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Encapsula os pedidos HTTP da feature de registos de producao.
/// </summary>
public sealed class RegistosProducaoService : ApiServiceBase
{
    /// <summary>
    /// Construtor do servico de registos de producao.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com o endpoint base da API.</param>
    public RegistosProducaoService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    /// <summary>
    /// Lista registos de producao de forma paginada.
    /// </summary>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com registos ou nulo quando a API nao devolve sucesso.</returns>
    public async Task<PagedResult<RegistoProducaoDto>?> GetAllAsync(int page, int pageSize)
    {
        try
        {
            using var response = await HttpClient.GetAsync($"api/RegistosProducao?page={page}&pageSize={pageSize}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await DeserializeAsync<PagedResult<RegistoProducaoDto>>(response);
        }
        catch (Exception ex) when (IsConnectivityException(ex))
        {
            throw CreateConnectivityException(ex, "Nao foi possivel contactar o backend para carregar os registos de producao.");
        }
    }

    /// <summary>
    /// Obtem o ultimo registo de producao para uma combinacao de fase e peca.
    /// </summary>
    /// <param name="faseId">Identificador da fase produtiva.</param>
    /// <param name="pecaId">Identificador da peca.</param>
    /// <returns>DTO do ultimo registo ou nulo quando ainda nao existe historial.</returns>
    public async Task<RegistoProducaoDto?> GetUltimoAsync(int faseId, int pecaId)
    {
        using var response = await HttpClient.GetAsync($"api/RegistosProducao/ultimo?faseId={faseId}&pecaId={pecaId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, $"Nao foi possivel obter o ultimo registo de producao da peca {pecaId} na fase {faseId}.");
        return await DeserializeAsync<RegistoProducaoDto>(response);
    }

    /// <summary>
    /// Cria um novo registo de producao para uma peca.
    /// </summary>
    /// <param name="pecaId">Identificador da peca produzida.</param>
    /// <param name="faseId">Identificador da fase executada.</param>
    /// <param name="gestorProducaoId">Operador responsavel pelo registo.</param>
    /// <param name="estadoProducao">Estado produtivo a registar.</param>
    /// <param name="maquinaId">Maquina associada quando aplicavel.</param>
    /// <param name="proximaFaseId">Proxima fase prevista quando aplicavel.</param>
    /// <param name="encomendaMoldeId">Associacao encomenda-molde ligada ao registo.</param>
    /// <returns>DTO do registo criado.</returns>
    public async Task<RegistoProducaoDto?> CreateAsync(
        int pecaId,
        int faseId,
        int gestorProducaoId,
        string estadoProducao,
        int? maquinaId = null,
        int? proximaFaseId = null,
        int? encomendaMoldeId = null)
    {
        var payload = new
        {
            Peca_id = pecaId,
            Fase_id = faseId,
            Maquina_id = maquinaId,
            Operador_id = gestorProducaoId,
            Estado_producao = estadoProducao,
            ProximaFase_id = proximaFaseId,
            EncomendaMolde_id = encomendaMoldeId
        };

        using var response = await HttpClient.PostAsJsonAsync("api/RegistosProducao", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel registar a producao da peca {pecaId}.");
        return await DeserializeAsync<RegistoProducaoDto>(response);
    }

    /// <summary>
    /// Cria uma ocorrencia operacional associada a uma peca.
    /// </summary>
    /// <param name="request">Dados funcionais da ocorrencia a registar.</param>
    /// <returns>Tarefa assincrona da operacao de registo.</returns>
    public async Task CreateOcorrenciaAsync(CreateOcorrenciaRequest request)
    {
        using var response = await HttpClient.PostAsJsonAsync("api/ocorrencias", request);
        await EnsureSuccessAsync(response, $"Nao foi possivel registar a ocorrencia da peca {request.PecaId}.");
    }
}
