using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Encapsula as operacoes HTTP da feature de encomendas no frontend.
/// </summary>
/// <remarks>
/// Centraliza listagens, detalhe, associacao encomenda-molde, fila global
/// e atualizacoes operacionais consumidas pelos ecras comerciais e de producao.
/// </remarks>
public sealed class EncomendasService : ApiServiceBase
{
    /// <summary>
    /// Construtor do servico de encomendas.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com o endpoint base da API.</param>
    public EncomendasService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    /// <summary>
    /// Lista todas as encomendas de forma paginada.
    /// </summary>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com encomendas ou nulo quando a API nao devolve sucesso.</returns>
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

    /// <summary>
    /// Lista encomendas ainda nao concluidas para contexto de producao.
    /// </summary>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com encomendas ativas ou nulo quando a API falha.</returns>
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

    /// <summary>
    /// Pesquisa encomendas nao concluidas por termo livre.
    /// </summary>
    /// <param name="searchTerm">Termo parcial aplicado a pesquisa.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com encomendas encontradas ou nulo quando a API falha.</returns>
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

    /// <summary>
    /// Obtem o detalhe resumido de uma encomenda.
    /// </summary>
    /// <param name="encomendaId">Identificador da encomenda.</param>
    /// <returns>DTO da encomenda ou nulo quando nao e encontrada.</returns>
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

    /// <summary>
    /// Lista os moldes associados a uma encomenda.
    /// </summary>
    /// <param name="encomendaId">Identificador da encomenda.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com associacoes encomenda-molde ou nulo quando a API falha.</returns>
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

    /// <summary>
    /// Lista as encomendas associadas a um molde.
    /// </summary>
    /// <param name="moldeId">Identificador do molde.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com associacoes encomenda-molde ou nulo quando a API falha.</returns>
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

    /// <summary>
    /// Lista a fila global de prioridades dos moldes.
    /// </summary>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com a fila global ou nulo quando a API falha.</returns>
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

    /// <summary>
    /// Lista moldes de encomendas confirmadas aptos para a area de desenho.
    /// </summary>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com moldes elegiveis para desenho ou nulo quando a API falha.</returns>
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

    /// <summary>
    /// Cria uma nova encomenda.
    /// </summary>
    /// <param name="clienteId">Identificador do cliente dono da encomenda.</param>
    /// <param name="numeroEncomendaCliente">Numero funcional da encomenda no cliente.</param>
    /// <param name="numeroProjetoCliente">Numero de projeto fornecido pelo cliente.</param>
    /// <param name="nomeServicoCliente">Nome do servico associado a encomenda.</param>
    /// <param name="nomeResponsavelCliente">Responsavel do lado do cliente.</param>
    /// <returns>DTO da encomenda criada.</returns>
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

    /// <summary>
    /// Associa um molde a uma encomenda com prioridade e data prevista.
    /// </summary>
    /// <param name="encomendaId">Identificador da encomenda.</param>
    /// <param name="moldeId">Identificador do molde.</param>
    /// <param name="quantidade">Quantidade encomendada.</param>
    /// <param name="prioridade">Prioridade global atribuida ao molde.</param>
    /// <param name="dataEntregaPrevista">Data prevista de entrega para o molde.</param>
    /// <returns>DTO da associacao criada.</returns>
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

    /// <summary>
    /// Atualiza o estado funcional de uma encomenda.
    /// </summary>
    /// <param name="encomendaId">Identificador da encomenda a atualizar.</param>
    /// <param name="estado">Novo estado funcional a aplicar.</param>
    /// <returns>Tarefa assincrona da atualizacao.</returns>
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

    /// <summary>
    /// Atualiza parcialmente uma associacao encomenda-molde.
    /// </summary>
    /// <param name="encomendaMoldeId">Identificador da associacao a atualizar.</param>
    /// <param name="quantidade">Nova quantidade quando aplicavel.</param>
    /// <param name="prioridade">Nova prioridade global quando aplicavel.</param>
    /// <param name="dataEntregaPrevista">Nova data prevista quando aplicavel.</param>
    /// <returns>Tarefa assincrona da atualizacao.</returns>
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

    /// <summary>
    /// Atualiza o estado operacional de um molde associado a encomenda.
    /// </summary>
    /// <param name="encomendaMoldeId">Identificador da associacao encomenda-molde.</param>
    /// <param name="estado">Novo estado operacional do molde na encomenda.</param>
    /// <returns>Tarefa assincrona da atualizacao.</returns>
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
