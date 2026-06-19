using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class EncomendasService : ApiServiceBase
{
    public EncomendasService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<PagedResult<EncomendaResumoDto>?> GetAllAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/encomendas?page={page}&pageSize={pageSize}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar as encomendas.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<EncomendaResumoDto>>(response);
    }

    public async Task<PagedResult<EncomendaResumoDto>?> GetEncomendasNaoConcluidasAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/encomendas/em-producao?page={page}&pageSize={pageSize}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar as encomendas em producao.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<EncomendaResumoDto>>(response);
    }

    public async Task<PagedResult<EncomendaResumoDto>?> SearchEncomendasNaoConcluidasAsync(string searchTerm, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/encomendas/em-producao/search?searchTerm={Uri.EscapeDataString(searchTerm.Trim())}&page={page}&pageSize={pageSize}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para pesquisar encomendas em producao.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<EncomendaResumoDto>>(response);
    }

    public async Task<EncomendaResumoDto?> GetByIdAsync(int encomendaId)
    {
        using var response = await HttpClient.GetAsync($"api/encomendas/{encomendaId}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar a encomenda.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<EncomendaResumoDto>(response);
    }

    public async Task<PagedResult<EncomendaMoldeDto>?> GetEncomendaMoldesByEncomendaIdAsync(int encomendaId, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/encomenda-moldes/por-encomenda/{encomendaId}?page={page}&pageSize={pageSize}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar os moldes da encomenda.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<EncomendaMoldeDto>>(response);
    }

    public async Task<PagedResult<EncomendaMoldeDto>?> GetEncomendaMoldesByMoldeIdAsync(int moldeId, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/encomenda-moldes/por-molde/{moldeId}?page={page}&pageSize={pageSize}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar as encomendas do molde.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<EncomendaMoldeDto>>(response);
    }

    public async Task<PagedResult<FilaGlobalMoldeItemDto>?> GetFilaGlobalMoldeAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/encomenda-moldes/fila-global?page={page}&pageSize={pageSize}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar a fila global de moldes.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<FilaGlobalMoldeItemDto>>(response);
    }

    public async Task<PagedResult<EncomendaMoldeDto>?> GetEncomendasConfirmadasParaDesenhoAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/encomenda-moldes/encomendas-confirmadas-para-desenho?page={page}&pageSize={pageSize}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar os moldes aptos para desenho.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<EncomendaMoldeDto>>(response);
    }

    public async Task<EncomendaResumoDto?> CreateAsync(
        int clienteId,
        string numeroEncomendaCliente,
        string? numeroProjetoCliente,
        string? nomeServicoCliente,
        string? nomeResponsavelCliente)
    {
        var payload = new
        {
            Cliente_id = clienteId,
            NumeroEncomendaCliente = numeroEncomendaCliente,
            NumeroProjetoCliente = numeroProjetoCliente,
            NomeServicoCliente = nomeServicoCliente,
            NomeResponsavelCliente = nomeResponsavelCliente
        };

        using var response = await HttpClient.PostAsJsonAsync("api/encomendas", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel criar a encomenda. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<EncomendaResumoDto>(response);
    }

    public async Task<EncomendaMoldeDto?> CreateEncomendaMoldeAsync(
        int encomendaId,
        int moldeId,
        int quantidade,
        int prioridade,
        DateTime dataEntregaPrevista)
    {
        var payload = new
        {
            Encomenda_id = encomendaId,
            Molde_id = moldeId,
            Quantidade = quantidade,
            Prioridade = prioridade,
            DataEntregaPrevista = dataEntregaPrevista
        };

        using var response = await HttpClient.PostAsJsonAsync("api/encomenda-moldes", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel associar o molde {moldeId} a encomenda {encomendaId}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<EncomendaMoldeDto>(response);
    }

    public async Task UpdateEstadoAsync(int encomendaId, string estado)
    {
        var payload = new
        {
            Estado = estado
        };

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/encomendas/{encomendaId}/estado")
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await HttpClient.SendAsync(request);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar o estado da encomenda {encomendaId}. Estado: {(int)response.StatusCode}");
    }

    public async Task UpdateEncomendaMoldeAsync(
        int encomendaMoldeId,
        int? quantidade = null,
        int? prioridade = null,
        DateTime? dataEntregaPrevista = null)
    {
        var payload = new
        {
            Quantidade = quantidade,
            Prioridade = prioridade,
            DataEntregaPrevista = dataEntregaPrevista
        };

        using var response = await HttpClient.PutAsJsonAsync($"api/encomenda-moldes/{encomendaMoldeId}", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar a associacao {encomendaMoldeId}. Estado: {(int)response.StatusCode}");
    }

    public async Task UpdateEncomendaMoldeEstadoAsync(int encomendaMoldeId, string estado)
    {
        var payload = new
        {
            Estado = estado
        };

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/encomenda-moldes/{encomendaMoldeId}/estado")
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await HttpClient.SendAsync(request);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar o estado operacional do molde {encomendaMoldeId}. Estado: {(int)response.StatusCode}");
    }
}
