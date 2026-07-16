using System.Net.Http.Headers;
using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Encapsula os pedidos HTTP da feature de pecas no frontend.
/// </summary>
/// <remarks>
/// Suporta criacao, importacao, pesquisa, atualizacao e operacoes de producao
/// sobre pecas associadas aos moldes.
/// </remarks>
public sealed class PecasService : ApiServiceBase
{
    /// <summary>
    /// Construtor do servico de pecas.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com o endpoint base da API.</param>
    public PecasService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    /// <summary>
    /// Cria uma nova peca para um molde.
    /// </summary>
    /// <param name="moldeId">Identificador do molde pai.</param>
    /// <param name="request">Dados funcionais da peca a criar.</param>
    /// <returns>DTO da peca criada.</returns>
    public async Task<PecaDto?> CreateAsync(int moldeId, PecaUpsertRequest request)
    {
        var payload = BuildCreatePayload(moldeId, request);
        using var response = await HttpClient.PostAsJsonAsync("api/pecas", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel criar a peca para o molde {moldeId}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<PecaDto>(response);
    }

    /// <summary>
    /// Importa pecas para um molde a partir de um ficheiro CSV.
    /// </summary>
    /// <param name="moldeId">Identificador do molde que recebe as pecas.</param>
    /// <param name="file">Ficheiro CSV selecionado pelo utilizador.</param>
    /// <returns>Resultado detalhado da importacao CSV.</returns>
    public async Task<ImportPecasCsvResultDto?> ImportCsvAsync(int moldeId, FileResult file)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using var stream = await file.OpenReadAsync();
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", string.IsNullOrWhiteSpace(file.FileName) ? $"molde-{moldeId}.csv" : file.FileName);

        using var response = await HttpClient.PostAsync($"api/pecas/por-molde/{moldeId}/importacao-csv", content);
        await EnsureSuccessAsync(response, $"Nao foi possivel importar pecas para o molde {moldeId}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<ImportPecasCsvResultDto>(response);
    }

    /// <summary>
    /// Lista pecas de um molde de forma paginada.
    /// </summary>
    /// <param name="moldeId">Identificador do molde.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com pecas do molde ou nulo quando a API falha.</returns>
    public async Task<PagedResult<PecaDto>?> GetByMoldeIdAsync(int moldeId, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/pecas/por-molde/{moldeId}?page={page}&pageSize={pageSize}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar as pecas do molde.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<PecaDto>>(response);
    }

    /// <summary>
    /// Lista pecas de um molde ainda sem pedido de material associado.
    /// </summary>
    /// <param name="moldeId">Identificador do molde.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <param name="searchTerm">Termo opcional para filtrar a lista.</param>
    /// <returns>Resultado paginado com pecas elegiveis ou nulo quando a API falha.</returns>
    public async Task<PagedResult<PecaDto>?> GetByMoldeIdWithoutPedidoMaterialAsync(int moldeId, int page, int pageSize, string? searchTerm = null)
    {
        var query = $"api/pecas/por-molde/{moldeId}/sem-pedido-material?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query += $"&searchTerm={Uri.EscapeDataString(searchTerm.Trim())}";

        using var response = await HttpClient.GetAsync(query);

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar as pecas do molde.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<PecaDto>>(response);
    }

    /// <summary>
    /// Lista pecas com material pendente de rececao para um molde.
    /// </summary>
    /// <param name="moldeId">Identificador do molde.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com pecas pendentes de rececao ou nulo quando a API falha.</returns>
    public async Task<PagedResult<PecaDto>?> GetByMoldeIdPendingMaterialReceiptAsync(int moldeId, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/pecas/por-molde/{moldeId}/pendentes-rececao-material?page={page}&pageSize={pageSize}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar as pecas pendentes de rececao.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<PecaDto>>(response);
    }

    /// <summary>
    /// Lista a fila de trabalho de pecas para a area de producao.
    /// </summary>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <param name="searchTerm">Termo opcional aplicado a pesquisa.</param>
    /// <param name="searchMode">Modo funcional da pesquisa no backend.</param>
    /// <returns>Resultado paginado com pecas disponiveis ou nulo quando a API nao devolve sucesso.</returns>
    public async Task<PagedResult<ProducaoPecaDisponivelItem>?> GetFilaTrabalhoAsync(
        int page,
        int pageSize,
        string? searchTerm,
        string searchMode,
        int? faseId = null)
    {
        var query = $"api/pecas/fila-trabalho?page={page}&pageSize={pageSize}&searchMode={Uri.EscapeDataString(searchMode)}";

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query += $"&searchTerm={Uri.EscapeDataString(searchTerm.Trim())}";

        if (faseId.HasValue)
            query += $"&faseId={faseId.Value}";

        try
        {
            using var response = await HttpClient.GetAsync(query);

            await ThrowIfAuthorizationFailureAsync(
                response,
                "Nao tens permissao para consultar a fila de trabalho de pecas.");

            if (!response.IsSuccessStatusCode)
                return null;

            return await DeserializeAsync<PagedResult<ProducaoPecaDisponivelItem>>(response);
        }
        catch (Exception ex) when (IsConnectivityException(ex))
        {
            throw CreateConnectivityException(ex, "Nao foi possivel contactar o backend para carregar a fila de trabalho das pecas.");
        }
    }

    /// <summary>
    /// Obtem uma peca pelo identificador.
    /// </summary>
    /// <param name="pecaId">Identificador da peca.</param>
    /// <returns>DTO da peca ou nulo quando nao e encontrada.</returns>
    public async Task<PecaDto?> GetByIdAsync(int pecaId)
    {
        using var response = await HttpClient.GetAsync($"api/pecas/{pecaId}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar a peca.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PecaDto>(response);
    }

    /// <summary>
    /// Atualiza os dados editaveis de uma peca.
    /// </summary>
    /// <param name="pecaId">Identificador da peca a atualizar.</param>
    /// <param name="request">Dados funcionais a aplicar na atualizacao.</param>
    /// <returns>Tarefa assincrona da atualizacao.</returns>
    public async Task UpdateAsync(int pecaId, PecaUpsertRequest request)
    {
        var payload = BuildUpdatePayload(request);
        using var response = await HttpClient.PutAsJsonAsync($"api/pecas/{pecaId}", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar a peca {pecaId}. Estado: {(int)response.StatusCode}");
    }

    /// <summary>
    /// Remove uma peca existente.
    /// </summary>
    /// <param name="pecaId">Identificador da peca a remover.</param>
    /// <returns>Tarefa assincrona da remocao.</returns>
    public async Task DeleteAsync(int pecaId)
    {
        using var response = await HttpClient.DeleteAsync($"api/pecas/{pecaId}");
        await EnsureSuccessAsync(response, $"Nao foi possivel eliminar a peca {pecaId}. Estado: {(int)response.StatusCode}");
    }

    /// <summary>
    /// Atualiza o estado de rececao de material de uma peca.
    /// </summary>
    /// <param name="pecaId">Identificador da peca.</param>
    /// <param name="materialRecebido">Novo estado de material recebido.</param>
    /// <returns>Tarefa assincrona da atualizacao.</returns>
    public async Task UpdateMaterialRecebidoAsync(int pecaId, bool materialRecebido)
    {
        var payload = new
        {
            MaterialRecebido = materialRecebido
        };

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/pecas/{pecaId}/material-recebido")
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await HttpClient.SendAsync(request);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar a rececao de material da peca {pecaId}. Estado: {(int)response.StatusCode}");
    }

    /// <summary>
    /// Atualiza a proxima fase produtiva de uma peca.
    /// </summary>
    /// <param name="pecaId">Identificador da peca.</param>
    /// <param name="proximaFaseId">Identificador da fase a definir como proxima etapa.</param>
    /// <returns>Tarefa assincrona da atualizacao.</returns>
    public async Task UpdateProximaFaseAsync(int pecaId, int proximaFaseId)
    {
        var payload = new
        {
            ProximaFase_id = proximaFaseId
        };

        using var response = await HttpClient.PutAsJsonAsync($"api/pecas/{pecaId}", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar a proxima fase da peca {pecaId}. Estado: {(int)response.StatusCode}");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static object BuildCreatePayload(int moldeId, PecaUpsertRequest request)
    {
        return new
        {
            NumeroPeca = NormalizeOptional(request.NumeroPeca),
            Designacao = request.Designacao.Trim(),
            Prioridade = request.Prioridade,
            Quantidade = request.Quantidade,
            Referencia = NormalizeOptional(request.Referencia),
            MaterialDesignacao = NormalizeOptional(request.MaterialDesignacao),
            TratamentoTermico = NormalizeOptional(request.TratamentoTermico),
            Massa = NormalizeOptional(request.Massa),
            Observacao = NormalizeOptional(request.Observacao),
            MaterialRecebido = request.MaterialRecebido,
            ProximaFase_id = request.ProximaFaseId,
            Molde_id = moldeId
        };
    }

    private static object BuildUpdatePayload(PecaUpsertRequest request)
    {
        return new
        {
            NumeroPeca = NormalizeOptional(request.NumeroPeca),
            Designacao = request.Designacao.Trim(),
            Prioridade = request.Prioridade,
            Quantidade = request.Quantidade,
            Referencia = NormalizeOptional(request.Referencia),
            MaterialDesignacao = NormalizeOptional(request.MaterialDesignacao),
            TratamentoTermico = NormalizeOptional(request.TratamentoTermico),
            Massa = NormalizeOptional(request.Massa),
            Observacao = NormalizeOptional(request.Observacao),
            ProximaFase_id = request.ProximaFaseId
        };
    }
}

/// <summary>
/// Representa os dados editaveis usados para criar ou atualizar uma peca.
/// </summary>
public sealed record PecaUpsertRequest
{
    public string Designacao { get; init; } = string.Empty;
    public int Prioridade { get; init; }
    public int Quantidade { get; init; }
    public int? ProximaFaseId { get; init; }
    public string? NumeroPeca { get; init; }
    public string? Referencia { get; init; }
    public string? MaterialDesignacao { get; init; }
    public string? TratamentoTermico { get; init; }
    public string? Massa { get; init; }
    public string? Observacao { get; init; }
    public bool MaterialRecebido { get; init; }
}
