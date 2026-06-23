using System.Net.Http.Headers;
using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class PecasService : ApiServiceBase
{
    public PecasService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<PecaDto?> CreateAsync(int moldeId, PecaUpsertRequest request)
    {
        var payload = BuildCreatePayload(moldeId, request);
        using var response = await HttpClient.PostAsJsonAsync("api/pecas", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel criar a peca para o molde {moldeId}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<PecaDto>(response);
    }

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

    public async Task<PagedResult<ProducaoPecaDisponivelItem>?> GetFilaTrabalhoAsync(
        int page,
        int pageSize,
        string? searchTerm,
        string searchMode)
    {
        var query = $"api/pecas/fila-trabalho?page={page}&pageSize={pageSize}&searchMode={Uri.EscapeDataString(searchMode)}";

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query += $"&searchTerm={Uri.EscapeDataString(searchTerm.Trim())}";

        using var response = await HttpClient.GetAsync(query);

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar a fila de trabalho de pecas.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<ProducaoPecaDisponivelItem>>(response);
    }

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

    public async Task UpdateAsync(int pecaId, PecaUpsertRequest request)
    {
        var payload = BuildUpdatePayload(request);
        using var response = await HttpClient.PutAsJsonAsync($"api/pecas/{pecaId}", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar a peca {pecaId}. Estado: {(int)response.StatusCode}");
    }

    public async Task DeleteAsync(int pecaId)
    {
        using var response = await HttpClient.DeleteAsync($"api/pecas/{pecaId}");
        await EnsureSuccessAsync(response, $"Nao foi possivel eliminar a peca {pecaId}. Estado: {(int)response.StatusCode}");
    }

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
