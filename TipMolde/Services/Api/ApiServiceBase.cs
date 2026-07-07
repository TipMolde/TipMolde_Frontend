using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TipMolde.Services;

/// <summary>
/// Fornece utilitarios comuns para os servicos HTTP do frontend.
/// </summary>
/// <remarks>
/// Normaliza desserializacao JSON, traducao de erros do backend e deteccao
/// de falhas de conectividade para reduzir duplicacao entre services.
/// </remarks>
public abstract class ApiServiceBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static ApiServiceBase()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>
    /// Construtor da classe base dos servicos HTTP.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com o endpoint base da API.</param>
    protected ApiServiceBase(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    /// <summary>
    /// Cliente HTTP partilhado pelos servicos derivados.
    /// </summary>
    protected HttpClient HttpClient { get; }

    /// <summary>
    /// Verifica se a excecao corresponde a uma falha de ligacao ou transporte.
    /// </summary>
    /// <param name="ex">Excecao capturada durante a chamada HTTP.</param>
    /// <returns>True quando a excecao indica indisponibilidade de conectividade; false caso contrario.</returns>
    protected static bool IsConnectivityException(Exception ex)
    {
        return ex is HttpRequestException or TaskCanceledException or COMException;
    }

    /// <summary>
    /// Cria uma excecao enriquecida com o endpoint ativo para falhas de conectividade.
    /// </summary>
    /// <param name="ex">Excecao original da camada de transporte.</param>
    /// <param name="operationDescription">Descricao funcional da operacao que falhou.</param>
    /// <returns>Excecao de operacao invalida pronta para ser mostrada pelo frontend.</returns>
    protected InvalidOperationException CreateConnectivityException(Exception ex, string operationDescription)
    {
        var baseUrl = HttpClient.BaseAddress?.ToString() ?? "desconhecida";
        return new InvalidOperationException(
            $"{operationDescription} Endpoint atual: {baseUrl} Detalhe tecnico: {ex.Message}",
            ex);
    }

    /// <summary>
    /// Desserializa o corpo JSON da resposta para o tipo pedido.
    /// </summary>
    /// <param name="response">Resposta HTTP com conteudo potencialmente JSON.</param>
    /// <returns>Objeto desserializado ou o valor por omissao quando o corpo vier vazio.</returns>
    protected static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(content))
            return default;

        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    /// <summary>
    /// Garante que a resposta HTTP foi bem-sucedida antes de continuar o fluxo.
    /// </summary>
    /// <param name="response">Resposta HTTP a validar.</param>
    /// <param name="fallbackMessage">Mensagem a usar quando o backend nao devolve detalhe util.</param>
    /// <returns>Tarefa concluida quando a resposta e valida.</returns>
    protected static async Task EnsureSuccessAsync(HttpResponseMessage response, string fallbackMessage)
    {
        if (response.IsSuccessStatusCode)
            return;

        throw await CreateApiExceptionAsync(response, fallbackMessage);
    }

    /// <summary>
    /// Interrompe o fluxo quando a API devolve falta de autorizacao.
    /// </summary>
    /// <param name="response">Resposta HTTP a inspecionar.</param>
    /// <param name="fallbackMessage">Mensagem de fallback para o erro devolvido ao utilizador.</param>
    /// <returns>Tarefa concluida quando nao existe erro de autorizacao.</returns>
    protected static async Task ThrowIfAuthorizationFailureAsync(HttpResponseMessage response, string fallbackMessage)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw await CreateApiExceptionAsync(response, fallbackMessage);
    }

    /// <summary>
    /// Converte uma resposta HTTP falhada numa excecao funcional com a melhor mensagem disponivel.
    /// </summary>
    /// <param name="response">Resposta HTTP falhada devolvida pela API.</param>
    /// <param name="fallbackMessage">Mensagem de fallback quando o corpo nao fornece detalhe util.</param>
    /// <returns>Excecao pronta para propagar ao view model.</returns>
    protected static async Task<InvalidOperationException> CreateApiExceptionAsync(HttpResponseMessage response, string fallbackMessage)
    {
        var content = await response.Content.ReadAsStringAsync();
        var message = ExtractApiErrorMessage(content);
        return new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? fallbackMessage : message);
    }

    /// <summary>
    /// Extrai a mensagem de erro mais relevante do corpo devolvido pela API.
    /// </summary>
    /// <param name="content">Conteudo bruto devolvido pela API.</param>
    /// <returns>Mensagem funcional quando encontrada; caso contrario, nulo.</returns>
    protected static string? ExtractApiErrorMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            using var document = JsonDocument.Parse(content);
            return TryExtractJsonErrorMessage(document.RootElement) ?? content;
        }
        catch (JsonException)
        {
            // If the backend returns plain text instead of JSON, fall back to raw content.
            return content;
        }
    }

    private static string? TryExtractJsonErrorMessage(JsonElement root)
    {
        return TryGetStringProperty(root, "detail")
            ?? TryExtractValidationError(root)
            ?? TryGetStringProperty(root, "title");
    }

    private static string? TryGetStringProperty(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element) ||
            element.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = element.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? TryExtractValidationError(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errorsElement) ||
            errorsElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in errorsElement.EnumerateObject())
        {
            var error = TryExtractFirstStringFromArray(property.Value);
            if (error is not null)
                return error;
        }

        return null;
    }

    private static string? TryExtractFirstStringFromArray(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
                continue;

            var value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
