using System.Net.Http.Json;
using System.Text.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class EncomendasService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public EncomendasService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<EncomendaResumoDto>?> GetAllAsync(int page, int pageSize)
    {
        using var response = await _httpClient.GetAsync(
            $"api/encomendas?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<EncomendaResumoDto>>(response);
    }

    public async Task<PagedResult<EncomendaResumoDto>?> GetEncomendasNaoConcluidasAsync(int page, int pageSize)
    {
        using var response = await _httpClient.GetAsync(
            $"api/encomendas/em-producao?page={page}&pageSize={pageSize}");
 
        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<EncomendaResumoDto>>(response);
    }

    public async Task<PagedResult<EncomendaResumoDto>?> SearchEncomendasNaoConcluidasAsync(string searchTerm, int page, int pageSize)
    {
        using var response = await _httpClient.GetAsync(
            $"api/encomendas/em-producao/search?searchTerm={Uri.EscapeDataString(searchTerm.Trim())}&page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<EncomendaResumoDto>>(response);
    }

    public async Task<EncomendaResumoDto?> GetByIdAsync(int encomendaId)
    {
        using var response = await _httpClient.GetAsync($"api/encomendas/{encomendaId}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<EncomendaResumoDto>(response);
    }

    public async Task<PagedResult<EncomendaMoldeDto>?> GetEncomendaMoldesByEncomendaIdAsync(int encomendaId, int page, int pageSize)
    {
        using var response = await _httpClient.GetAsync(
            $"api/encomenda-moldes/por-encomenda/{encomendaId}?page={page}&pageSize={pageSize}");

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

        using var response = await _httpClient.PostAsJsonAsync("api/encomendas", payload);

        if (!response.IsSuccessStatusCode)
            throw await CreateApiExceptionAsync(response, $"Nao foi possivel criar a encomenda. Estado: {(int)response.StatusCode}");

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

        using var response = await _httpClient.PostAsJsonAsync("api/encomenda-moldes", payload);

        if (!response.IsSuccessStatusCode)
            throw await CreateApiExceptionAsync(response, $"Nao foi possivel associar o molde {moldeId} a encomenda {encomendaId}. Estado: {(int)response.StatusCode}");

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

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw await CreateApiExceptionAsync(response, $"Nao foi possivel atualizar o estado da encomenda {encomendaId}. Estado: {(int)response.StatusCode}");
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

        using var response = await _httpClient.PutAsJsonAsync($"api/encomenda-moldes/{encomendaMoldeId}", payload);

        if (!response.IsSuccessStatusCode)
            throw await CreateApiExceptionAsync(response, $"Nao foi possivel atualizar a associacao {encomendaMoldeId}. Estado: {(int)response.StatusCode}");
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private static async Task<InvalidOperationException> CreateApiExceptionAsync(HttpResponseMessage response, string fallbackMessage)
    {
        var content = await response.Content.ReadAsStringAsync();
        var message = ExtractApiErrorMessage(content);
        return new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? fallbackMessage : message);
    }

    private static string? ExtractApiErrorMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            if (root.TryGetProperty("detail", out var detailElement) &&
                detailElement.ValueKind == JsonValueKind.String)
            {
                var detail = detailElement.GetString();
                if (!string.IsNullOrWhiteSpace(detail))
                    return detail;
            }

            if (root.TryGetProperty("title", out var titleElement) &&
                titleElement.ValueKind == JsonValueKind.String)
            {
                var title = titleElement.GetString();
                if (!string.IsNullOrWhiteSpace(title))
                    return title;
            }
        }
        catch (JsonException)
        {
            // If the backend returns plain text instead of JSON, fall back to raw content.
        }

        return content;
    }
}
