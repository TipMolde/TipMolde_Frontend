using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TipMolde.Services;

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

    protected ApiServiceBase(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    protected HttpClient HttpClient { get; }

    protected static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(content))
            return default;

        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    protected static async Task EnsureSuccessAsync(HttpResponseMessage response, string fallbackMessage)
    {
        if (response.IsSuccessStatusCode)
            return;

        throw await CreateApiExceptionAsync(response, fallbackMessage);
    }

    protected static async Task ThrowIfAuthorizationFailureAsync(HttpResponseMessage response, string fallbackMessage)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw await CreateApiExceptionAsync(response, fallbackMessage);
    }

    protected static async Task<InvalidOperationException> CreateApiExceptionAsync(HttpResponseMessage response, string fallbackMessage)
    {
        var content = await response.Content.ReadAsStringAsync();
        var message = ExtractApiErrorMessage(content);
        return new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? fallbackMessage : message);
    }

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
