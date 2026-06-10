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

    public async Task<PecaDto?> CreateAsync(
        int moldeId,
        string designacao,
        int prioridade,
        int quantidade,
        int? proximaFaseId = null,
        string? numeroPeca = null,
        string? referencia = null,
        string? materialDesignacao = null,
        string? tratamentoTermico = null,
        string? massa = null,
        string? observacao = null,
        bool materialRecebido = false)
    {
        var payload = new
        {
            NumeroPeca = NormalizeOptional(numeroPeca),
            Designacao = designacao.Trim(),
            Prioridade = prioridade,
            Quantidade = quantidade,
            Referencia = NormalizeOptional(referencia),
            MaterialDesignacao = NormalizeOptional(materialDesignacao),
            TratamentoTermico = NormalizeOptional(tratamentoTermico),
            Massa = NormalizeOptional(massa),
            Observacao = NormalizeOptional(observacao),
            MaterialRecebido = materialRecebido,
            ProximaFase_id = proximaFaseId,
            Molde_id = moldeId
        };

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

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<PecaDto>>(response);
    }

    public async Task<PecaDto?> GetByIdAsync(int pecaId)
    {
        using var response = await HttpClient.GetAsync($"api/pecas/{pecaId}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PecaDto>(response);
    }

    public async Task UpdateAsync(
        int pecaId,
        string designacao,
        int prioridade,
        int quantidade,
        int? proximaFaseId = null,
        string? numeroPeca = null,
        string? referencia = null,
        string? materialDesignacao = null,
        string? tratamentoTermico = null,
        string? massa = null,
        string? observacao = null)
    {
        var payload = new
        {
            NumeroPeca = NormalizeOptional(numeroPeca),
            Designacao = designacao.Trim(),
            Prioridade = prioridade,
            Quantidade = quantidade,
            Referencia = NormalizeOptional(referencia),
            MaterialDesignacao = NormalizeOptional(materialDesignacao),
            TratamentoTermico = NormalizeOptional(tratamentoTermico),
            Massa = NormalizeOptional(massa),
            Observacao = NormalizeOptional(observacao),
            ProximaFase_id = proximaFaseId
        };

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

        using var response = await HttpClient.PutAsJsonAsync($"api/pecas/{pecaId}", payload);
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
}
