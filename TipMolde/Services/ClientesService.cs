using System.Net.Http.Json;
using System.Text.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class ClientesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public ClientesService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<ClienteDto>?> GetClientesAsync(int page, int pageSize)
    {
        using var response = await _httpClient.GetAsync($"api/clientes?page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<ClienteDto>>(response);
    }

    public async Task<PagedResult<ClienteDto>?> SearchByNameAsync(string searchTerm, int page, int pageSize)
    {
        using var response = await _httpClient.GetAsync(
            $"api/clientes/search/by-name?searchTerm={Uri.EscapeDataString(searchTerm)}&page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<ClienteDto>>(response);
    }

    public async Task<PagedResult<ClienteDto>?> SearchBySiglaAsync(string searchTerm, int page, int pageSize)
    {
        using var response = await _httpClient.GetAsync(
            $"api/clientes/search/by-sigla?searchTerm={Uri.EscapeDataString(searchTerm)}&page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<ClienteDto>>(response);
    }

    public async Task<ClienteComEncomendasDto?> GetClienteWithEncomendasAsync(int clienteId)
    {
        using var response = await _httpClient.GetAsync($"api/clientes/{clienteId}/encomendas");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<ClienteComEncomendasDto>(response);
    }

    public async Task<ClienteDto?> GetByIdAsync(int clienteId)
    {
        using var response = await _httpClient.GetAsync($"api/clientes/{clienteId}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<ClienteDto>(response);
    }

    public async Task<ClienteDto?> CreateAsync(
        string nome,
        string nif,
        string sigla,
        string? pais,
        string? email,
        string? telefone)
    {
        var payload = new
        {
            nome,
            nif,
            sigla,
            pais,
            email,
            telefone
        };

        using var response = await _httpClient.PostAsJsonAsync("api/clientes", payload);

        if (!response.IsSuccessStatusCode)
            throw await CreateApiExceptionAsync(response, $"Nao foi possivel criar o cliente. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<ClienteDto>(response);
    }

    public async Task UpdateAsync(
        int clienteId,
        string nome,
        string nif,
        string sigla,
        string? pais,
        string? email,
        string? telefone)
    {
        var payload = new
        {
            nome,
            nif,
            sigla,
            pais,
            email,
            telefone
        };

        using var response = await _httpClient.PutAsJsonAsync($"api/clientes/{clienteId}", payload);

        if (!response.IsSuccessStatusCode)
            throw await CreateApiExceptionAsync(response, $"Nao foi possivel atualizar o cliente com ID {clienteId}. Estado: {(int)response.StatusCode}");
    }

    public async Task DeleteAsync(int clienteId)
    {
        using var response = await _httpClient.DeleteAsync($"api/clientes/{clienteId}");

        if (!response.IsSuccessStatusCode)
            throw await CreateApiExceptionAsync(response, $"Nao foi possivel eliminar o cliente com ID {clienteId}. Estado: {(int)response.StatusCode}");
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
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

            if (root.TryGetProperty("errors", out var errorsElement) &&
                errorsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in errorsElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in property.Value.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                var error = item.GetString();
                                if (!string.IsNullOrWhiteSpace(error))
                                    return error;
                            }
                        }
                    }
                }
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
