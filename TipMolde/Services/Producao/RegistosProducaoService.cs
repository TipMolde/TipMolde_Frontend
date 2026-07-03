using System.Net;
using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class RegistosProducaoService : ApiServiceBase
{
    public RegistosProducaoService(HttpClient httpClient)
        : base(httpClient)
    {
    }

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

    public async Task<RegistoProducaoDto?> GetUltimoAsync(int faseId, int pecaId)
    {
        using var response = await HttpClient.GetAsync($"api/RegistosProducao/ultimo?faseId={faseId}&pecaId={pecaId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, $"Nao foi possivel obter o ultimo registo de producao da peca {pecaId} na fase {faseId}.");
        return await DeserializeAsync<RegistoProducaoDto>(response);
    }

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

    public async Task CreateOcorrenciaAsync(CreateOcorrenciaRequest request)
    {
        using var response = await HttpClient.PostAsJsonAsync("api/ocorrencias", request);
        await EnsureSuccessAsync(response, $"Nao foi possivel registar a ocorrencia da peca {request.PecaId}.");
    }
}
